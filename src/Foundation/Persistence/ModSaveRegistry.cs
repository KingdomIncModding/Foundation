using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed partial class ModSaveRegistry : IModSaveMigrations
    {
        internal const string RootKey = "FoundationModData";
        // Injectable reporter keeps regression fixtures from being classified as
        // unexpected BepInEx runtime errors while production uses normal logging.
        readonly Action<Exception> report;
        internal ModSaveRegistry(Action<Exception> reporter = null) { report = reporter ?? FoundationApi.Report; }
        readonly Dictionary<string, Registration> registrations = new Dictionary<string, Registration>(StringComparer.Ordinal);
        readonly Dictionary<string, Stored> retained = new Dictionary<string, Stored>(StringComparer.Ordinal);
        // An unknown or malformed owner entry must survive load/save unchanged.
        // This is separate from schema-aware entries so invalid data cannot reach callbacks.
        readonly Dictionary<string, JsonValue> opaque = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
        JsonValue opaqueRoot = JsonValue.Null;
        bool hasLoaded;


        /// <summary>Gets or sets the retained mod ids value.</summary>
        public IReadOnlyList<string> RetainedModIds => Array.AsReadOnly(retained.Keys.Concat(opaque.Keys).Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        public IDisposable Register(string modId, int schemaVersion, Func<JsonValue> save, Action<int, JsonValue> load)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Schema version must be >= 1.");
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (load == null) throw new ArgumentNullException(nameof(load));
            if (registrations.ContainsKey(modId)) throw new ArgumentException("Save provider already registered: " + modId, nameof(modId));
            var registration = new Registration(this) { ModId = modId, Version = schemaVersion, Save = save, Load = load };
            registrations.Add(modId, registration);
            if (hasLoaded)
            {
                if (retained.TryGetValue(modId, out var previous)) Restore(registration, previous);
                else if (!opaqueRoot.IsNull || opaque.ContainsKey(modId)) registration.LoadFailed = true;
                else Restore(registration, null);
            }
            return registration;
        }
        /// <summary>Registers migrating.</summary>
        public IDisposable RegisterMigrating(string modId, int schemaVersion, Func<JsonValue> save,
            Action<int, JsonValue> load, Func<int, JsonValue, JsonValue> migrate)
        {
            if (load == null) throw new ArgumentNullException(nameof(load));
            if (migrate == null) throw new ArgumentNullException(nameof(migrate));
            return Register(modId, schemaVersion, save, (previousVersion, original) =>
            {
                // Do not migrate an absent kingdom payload, or data written by a
                // newer provider: both must retain their existing semantics.
                if (previousVersion == 0 || previousVersion >= schemaVersion)
                {
                    load(previousVersion, original);
                    return;
                }
                // Work on a deep copy. Failed migration leaves the retained
                // saved JSON intact and suppresses writes for this provider.
                var converted = migrate(previousVersion, Clone(original));
                load(schemaVersion, Clone(converted));
            });
        }
        /// <summary>Attempts to get last loaded.</summary>
        public bool TryGetLastLoaded(string modId, out int schemaVersion, out JsonValue state)
        {
            if (modId != null && retained.TryGetValue(modId, out var saved))
            {
                schemaVersion = saved.Version; state = Clone(saved.Data); return true;
            }
            schemaVersion = 0; state = JsonValue.Null; return false;
        }
        internal void NewKingdom()
        {
            retained.Clear(); opaque.Clear(); opaqueRoot = JsonValue.Null; hasLoaded = true;
            foreach (var registration in registrations.Values.ToArray())
                Restore(registration, null);
        }
        internal void LoadState(JsonObject root)
        {
            retained.Clear(); opaque.Clear(); opaqueRoot = JsonValue.Null; hasLoaded = true;
            if (root != null && !root[RootKey].IsNull && !root[RootKey].IsJsonObject)
            {
                // A future Foundation revision might change the entire root format.
                // Never replace an unknown root with the current per-mod format.
                opaqueRoot = Clone(root[RootKey]);
                FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                    "Unrecognized FoundationModData root preserved; provider data is read-only for this save.");
            }
            if (root != null && root[RootKey].IsJsonObject)
            {
                foreach (var pair in root[RootKey].AsJsonObject)
                {
                    try
                    {
                        var entry = pair.Value;
                        if (string.IsNullOrEmpty(pair.Key)) continue;
                        if (!entry.IsJsonObject || !entry["Version"].IsInteger || entry["Version"].AsInteger < 1 ||
                            !entry.AsJsonObject.ContainsKey("Data"))
                        {
                            opaque[pair.Key] = Clone(entry);
                            FoundationApi.Diagnostics.Write(pair.Key, FoundationDiagnosticLevel.Warning,
                                "Unknown mod save format preserved without interpreting it.");
                            continue;
                        }
                        retained[pair.Key] = new Stored { Version = entry["Version"].AsInteger, Data = Clone(entry["Data"]) };
                    }
                    catch (Exception ex)
                    {
                        // A malformed mod payload cannot take down the native save loader.
                        // If even the payload cannot be copied, avoid claiming preservation.
                        try { opaque[pair.Key] = Clone(pair.Value); }
                        catch (Exception copyError) { report(copyError); }
                        report(new InvalidOperationException("Invalid save entry for mod " + pair.Key, ex));
                    }
                }
            }
            foreach (var registration in registrations.Values.ToArray())
            {
                if (!opaqueRoot.IsNull) { registration.LoadFailed = true; continue; }
                if (retained.TryGetValue(registration.ModId, out var saved)) Restore(registration, saved);
                else if (!opaque.ContainsKey(registration.ModId)) Restore(registration, null);
                else
                {
                    // The provider must not replace data it cannot understand.
                    registration.LoadFailed = true;
                    FoundationApi.Diagnostics.Write(registration.ModId, FoundationDiagnosticLevel.Warning,
                        "Unrecognized save entry retained; provider load/save skipped to prevent data loss.");
                }
            }
        }
        void Restore(Registration registration, Stored stored)
        {
            registration.LoadFailed = false;
            try { registration.Load(stored?.Version ?? 0, stored == null ? JsonValue.Null : Clone(stored.Data)); }
            catch (Exception ex)
            {
                registration.LoadFailed = true;
                report(new InvalidOperationException(
                    "Mod save-load callback failed: " + registration.ModId + "; previous save data will be preserved", ex));
            }
        }
        internal void WriteState(JsonObject root)
        {
            if (root == null) return;
            if (!opaqueRoot.IsNull)
            {
                root[RootKey] = Clone(opaqueRoot);
                return;
            }
            // Clone registered values before publishing them: one bad producer must not corrupt another mod's save.
            foreach (var registration in registrations.Values.ToArray())
            {
                if (registration.LoadFailed)
                {
                    FoundationApi.Diagnostics.Write(registration.ModId, FoundationDiagnosticLevel.Warning,
                        "Skipping save callback because restoring this provider failed; retaining original data.");
                    continue;
                }
                // A user who launches an older mod must not unknowingly downgrade
                // a newer save schema and destroy fields that mod cannot interpret.
                if (retained.TryGetValue(registration.ModId, out var newer) && newer.Version > registration.Version)
                {
                    FoundationApi.Diagnostics.Write(registration.ModId, FoundationDiagnosticLevel.Warning,
                        "Preserving newer mod save schema " + newer.Version + " (provider supports " + registration.Version + ").");
                    continue;
                }
                try
                {
                    var data = Clone(registration.Save());
                    retained[registration.ModId] = new Stored { Version = registration.Version, Data = data };
                }
                catch (Exception ex)
                {
                    // Keep the last good serialized value, including from an earlier schema.
                    report(new InvalidOperationException("Mod save callback failed: " + registration.ModId + "; retaining previous data", ex));
                }
            }
            if (retained.Count == 0 && opaque.Count == 0) return;
            var all = new JsonObject();
            foreach (var pair in opaque) all[pair.Key] = Clone(pair.Value);
            foreach (var pair in retained)
                all[pair.Key] = new JsonObject { { "Version", pair.Value.Version }, { "Data", Clone(pair.Value.Data) } };
            root[RootKey] = all;
        }
        internal static JsonValue Clone(JsonValue original) => original.IsNull ? JsonValue.Null : JsonValue.Parse(original.ToString());
    }
}
