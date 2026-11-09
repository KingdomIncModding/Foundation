using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Retains purchased technology IDs from a save when their definitions were not
    /// registered for this launch. The game's native GamePlayer.FromJson silently skips
    /// unknown IDs, and GamePlayer.ToJson would otherwise drop them on the next save.
    /// No missing technology is simulated, purchased or unlocked at runtime.
    /// </summary>
    internal static partial class MissingResearchRecovery
    {
        static readonly HashSet<string> unknown = new HashSet<string>(StringComparer.Ordinal);
        static readonly Dictionary<string, JsonValue> unknownNotifications =
            new Dictionary<string, JsonValue>(StringComparer.Ordinal);



        internal static IReadOnlyList<string> RetainedIds => Array.AsReadOnly(unknown.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        internal static IReadOnlyList<string> RetainedNotificationIds => Array.AsReadOnly(
            unknownNotifications.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray());

        internal static void Clear() { unknown.Clear(); unknownNotifications.Clear(); }

        internal static HashSet<string> Inspect(JsonObject saved)
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            if (saved == null || !saved["Player"].IsJsonObject) return found;
            var player = saved["Player"];
            if (!player["Tech"].IsJsonArray) return found;
            foreach (var candidate in player["Tech"].AsJsonArray)
            {
                if (!candidate.IsString) continue;
                var id = candidate.AsString;
                if (!string.IsNullOrWhiteSpace(id) && FoundationApi.Progression.FindTech(id) == null)
                    found.Add(id);
            }
            return found;
        }

        /// <summary>
        /// Vanilla GamePlayer.FromJson calls Dictionary&lt;TechNode,int&gt;.Add with
        /// a null TechNode for unknown keys in NewTechCount and aborts kingdom load.
        /// Remove ONLY unknown keys from the in-memory, already validated save JSON.
        /// Store them for future saves and restore the original JSON on load failure.
        /// </summary>
        internal static LoadSnapshot PrepareLoad(JsonObject saved)
        {
            var snapshot = new LoadSnapshot { MissingIds = Inspect(saved) };
            if (saved == null || !saved["Player"].IsJsonObject) return snapshot;
            var player = saved["Player"].AsJsonObject;
            if (!player["NewTechCount"].IsJsonObject) return snapshot;
            var counts = player["NewTechCount"].AsJsonObject;
            var keys = new List<string>();
            foreach (var pair in counts) keys.Add(pair.Key);
            foreach (var key in keys)
            {
                if (FoundationApi.Progression.FindTech(key) != null) continue;
                snapshot.MissingNotifications[key] = ModSaveRegistry.Clone(counts[key]);
                counts.Remove(key);
            }
            return snapshot;
        }

        internal static void RestoreInputOnFailure(JsonObject input, LoadSnapshot snapshot)
        {
            if (snapshot == null || snapshot.MissingNotifications.Count == 0 ||
                input == null || !input["Player"].IsJsonObject) return;
            var player = input["Player"].AsJsonObject;
            if (!player["NewTechCount"].IsJsonObject) return;
            var counts = player["NewTechCount"].AsJsonObject;
            foreach (var pair in snapshot.MissingNotifications)
                counts[pair.Key] = ModSaveRegistry.Clone(pair.Value);
        }

        internal static void Loaded(LoadSnapshot snapshot)
        {
            Loaded(snapshot?.MissingIds);
            unknownNotifications.Clear();
            if (snapshot == null) return;
            foreach (var pair in snapshot.MissingNotifications)
                unknownNotifications[pair.Key] = ModSaveRegistry.Clone(pair.Value);
            if (unknownNotifications.Count > 0)
                FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                    "Retained " + unknownNotifications.Count + " unavailable native research notification counters.");
        }

        internal static void Loaded(IEnumerable<string> missing)
        {
            unknown.Clear();
            if (missing == null) return;
            foreach (var id in missing) unknown.Add(id);
            if (unknown.Count > 0)
                FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                    "Retained " + unknown.Count + " unavailable researched technology IDs for future saves.");
        }

        internal static void Extend(JsonObject document)
        {
            if (document == null || !document["Player"].IsJsonObject) return;
            var player = document["Player"].AsJsonObject;
            if (unknown.Count > 0 && player["Tech"].IsJsonArray)
            {
                var tech = player["Tech"].AsJsonArray;
                var existing = new HashSet<string>(StringComparer.Ordinal);
                foreach (var value in tech)
                    if (value.IsString) existing.Add(value.AsString);
                foreach (var id in unknown.OrderBy(x => x, StringComparer.Ordinal))
                    if (existing.Add(id)) tech.Add(id);
            }
            if (unknownNotifications.Count > 0 && player["NewTechCount"].IsJsonObject)
            {
                var counts = player["NewTechCount"].AsJsonObject;
                foreach (var pair in unknownNotifications)
                    if (!counts.ContainsKey(pair.Key)) counts[pair.Key] = ModSaveRegistry.Clone(pair.Value);
            }
        }
    }
}
