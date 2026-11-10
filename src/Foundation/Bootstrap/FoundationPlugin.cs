using System;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Util;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        void VerifyFoundationCore()
        {
            const string owner = "urakka.foundation.test-services";
            // Typed events and deterministic owner cleanup.
            var events = new FoundationEventBus();
            int calls = 0;
            var firstListener = events.Subscribe<int>(owner, _ => calls++);
            var secondListener = events.Subscribe<int>(owner, _ => calls++);
            events.Publish(12);
            Check(calls == 2, "Event bus delivers typed messages to each subscriber");
            Check(events.Subscriptions.Count == 2 && events.RemoveOwner(owner) == 2, "Event bus tracks and clears owner registrations");
            events.Publish(14);
            Check(calls == 2 && events.Subscriptions.Count == 0, "Removed event subscriptions do not run");
            firstListener.Dispose(); secondListener.Dispose();

            // Signal registry: named sources and exposed property IDs must round-trip.
            var signals = new SignalRegistry();
            int state = 4, eventsSeen = 0;
            var source = signals.Register(new SignalSourceDefinition(owner, owner + ".example", "Example", new[]
            {
                new SignalPropertyDefinition("counter", "Counter", typeof(int), () => state, value => state = (int)value),
                new SignalPropertyDefinition("enabled", "Enabled", typeof(bool), () => true)
            }));
            Check(signals.TryGet(source.Id, out var found) && found.Properties.Count == 2, "Signal source and exposed properties are discoverable");
            Check(source.TryGetProperty("counter", out var value) && (int)value.Read() == 4 && value.CanWrite, "Signal property reads current data");
            value.Changed += _ => eventsSeen++;
            value.Write(5);
            Check(state == 5 && eventsSeen == 1, "Writable signal publishes one change notification");
            Check(source.TryGetProperty("enabled", out var readOnly) && !readOnly.CanWrite, "Read-only property does not expose a setter");
            Throws<InvalidOperationException>(() => readOnly.Write(true), "Read-only signal writes are rejected");
            Throws<ArgumentException>(() => signals.Register(new SignalSourceDefinition(owner, source.Id, "Duplicate", new SignalPropertyDefinition[0])), "Duplicate signal IDs are rejected");
            signals.Poll(); state = 9; signals.Poll();
            Check(eventsSeen == 2, "Externally changed signal getter is detected by sampling");
            source.Dispose();
            Check(signals.Sources.Count == 0 && !signals.TryGet(owner + ".example", out _), "Disposed signal source is undiscoverable");

            // Per-kingdom data retains unknown mod entries and supports versioned callbacks.
            var saves = new ModSaveRegistry();
            int persisted = 17, restored = -1, loadedSchema = -1;
            var handle = saves.Register(owner, 2, () => new JsonObject { { "Count", persisted } },
                (schema, json) => { loadedSchema = schema; restored = schema > 0 ? json["Count"].AsInteger : -1; });
            var jsonSave = new JsonObject();
            saves.WriteState(jsonSave);
            Check(jsonSave[ModSaveRegistry.RootKey].IsJsonObject && jsonSave[ModSaveRegistry.RootKey][owner]["Version"].AsInteger == 2,
                "Per-kingdom mod data serialized with a schema version");
            var extra = jsonSave[ModSaveRegistry.RootKey].AsJsonObject;
            extra["some.uninstalled.mod"] = new JsonObject { { "Version", 7 }, { "Data", new JsonObject { { "Unknown", 23 } } } };
            saves.LoadState(jsonSave);
            Check(loadedSchema == 2 && restored == 17 && saves.RetainedModIds.Count == 2, "Saved data loaded and unknown mod data retained");
            var roundTrip = new JsonObject();
            saves.WriteState(roundTrip);
            Check(roundTrip[ModSaveRegistry.RootKey]["some.uninstalled.mod"]["Data"]["Unknown"].AsInteger == 23,
                "Unknown mod data survives a subsequent save");
            // A provider that cannot restore data must never overwrite it with its
            // default state on the next autosave. Preserve opaque future formats too.
            const string fragileOwner = "urakka.foundation.test-loadfailure";
            int expectedMigrationFailures = 0;
            var fragile = new ModSaveRegistry(_ => expectedMigrationFailures++);
            fragile.Register(fragileOwner, 2, () => new JsonObject { { "Count", -999 } },
                (schema, value) => { if (schema > 0) throw new InvalidOperationException("Simulated migration failure"); });
            var previous = new JsonObject
            {
                { ModSaveRegistry.RootKey, new JsonObject
                  {
                      { fragileOwner, new JsonObject { { "Version", 2 }, { "Data", new JsonObject { { "Count", 12345 } } } } },
                      { "unavailable.future.mod", new JsonObject { { "Format", "experimental" }, { "Blob", "do not drop" } } }
                  }
                }
            };
            fragile.LoadState(previous);
            var protectedRoundTrip = new JsonObject();
            fragile.WriteState(protectedRoundTrip);
            Check(protectedRoundTrip[ModSaveRegistry.RootKey][fragileOwner]["Data"]["Count"].AsInteger == 12345 &&
                expectedMigrationFailures == 1,
                "Failed mod save migration is reported once and retains original data on subsequent saves");
            Check(protectedRoundTrip[ModSaveRegistry.RootKey]["unavailable.future.mod"]["Blob"].AsString == "do not drop",
                "Unrecognized future mod save format survives without interpretation");
            // If a future version changes the entire FoundationModData container,
            // preserve it instead of replacing it with our older format.
            var futureRoot = new JsonObject { { ModSaveRegistry.RootKey, new JsonArray { "future-root-format", 99 } } };
            fragile.LoadState(futureRoot);
            var rootRoundTrip = new JsonObject();
            fragile.WriteState(rootRoundTrip);
            Check(rootRoundTrip[ModSaveRegistry.RootKey].IsJsonArray &&
                rootRoundTrip[ModSaveRegistry.RootKey][0].AsString == "future-root-format",
                "Unrecognized future FoundationModData root survives a save roundtrip");

            // Late provider registration must not clobber the unknown payload either.
            var lateProvider = new ModSaveRegistry();
            lateProvider.LoadState(previous);
            lateProvider.Register("unavailable.future.mod", 1, () => new JsonObject { { "Blob", "lost" } },
                (schema, value) => { });
            var lateRoundTrip = new JsonObject();
            lateProvider.WriteState(lateRoundTrip);
            Check(lateRoundTrip[ModSaveRegistry.RootKey]["unavailable.future.mod"]["Blob"].AsString == "do not drop",
                "Late registration cannot overwrite an unrecognized save entry");
            handle.Dispose(); saves.NewKingdom();
            Check(saves.RetainedModIds.Count == 0, "Creating a new kingdom clears prior saved mod data");

            // Native GamePlayer.FromJson discards unknown Tech IDs. Foundation must
            // carry them forward in the normal Player.Tech array without unlocking.
            var oldUnknown = MissingResearchRecovery.RetainedIds;
            try
            {
                const string missingResearch = "urakka.foundation.tests.uninstalled-research";
                var missingDoc = new JsonObject { { "Player", new JsonObject
                    {
                        { "Tech", new JsonArray { "mine", missingResearch, missingResearch } },
                        { "NewTechCount", new JsonObject { { "mine", 5 }, { missingResearch, 9 } } }
                    } } };
                var missingIds = MissingResearchRecovery.Inspect(missingDoc);
                Check(missingIds.Count == 1 && missingIds.Contains(missingResearch),
                    "Research recovery discovers only unavailable, unique technology IDs");
                var loadSnapshot = MissingResearchRecovery.PrepareLoad(missingDoc);
                Check(loadSnapshot.MissingNotifications.Count == 1 &&
                    !missingDoc["Player"]["NewTechCount"].AsJsonObject.ContainsKey(missingResearch),
                    "Unknown native research counters are stripped before vanilla GamePlayer.FromJson");
                MissingResearchRecovery.RestoreInputOnFailure(missingDoc, loadSnapshot);
                Check(missingDoc["Player"]["NewTechCount"][missingResearch].AsInteger == 9,
                    "Failed kingdom load restores its in-memory research counters");
                MissingResearchRecovery.PrepareLoad(missingDoc);
                missingDoc["Player"].AsJsonObject["Tech"] = new JsonArray { "mine" };
                MissingResearchRecovery.Loaded(loadSnapshot);
                MissingResearchRecovery.Extend(missingDoc);
                MissingResearchRecovery.Extend(missingDoc);
                Check(missingDoc["Player"]["Tech"].AsJsonArray.Count == 2 &&
                    missingDoc["Player"]["Tech"].AsJsonArray.Any(v => v.IsString && v.AsString == missingResearch),
                    "Missing research survives repeated saves without duplicating IDs");
                Check(missingDoc["Player"]["NewTechCount"][missingResearch].AsInteger == 9 &&
                    missingDoc["Player"]["NewTechCount"]["mine"].AsInteger == 5,
                    "Unavailable research counters preserved without overwriting native counters");
            }
            finally { MissingResearchRecovery.Loaded(oldUnknown); }

            if (Assets.Source.Player.GamePlayer.Current != null &&
                Assets.Source.Player.GamePlayer.Current.Flows.Count > 0)
            {
                // SaveCurrentState's secret is calculated by SeedGenerator over the
                // complete JSON excluding Secret. Native saves with Foundation data
                // must remain compatible with this exact native calculation.
                var stateDocument = SaveGame.SaveCurrentState();
                var storedDigest = stateDocument["Secret"].AsString;
                stateDocument.Remove("Secret");
                Check(storedDigest == new SeedGenerator().Add(stateDocument).Hash.ToString(),
                    "Extended native save retains the game's exact FNV checksum");
            }

            var selector = new ModConfigSignalSelector("Source", new SignalSelection(null, null), null);
            selector.Value = new SignalSelection(owner + ".example", "counter");
            selector.Cancel();
            Check(selector.Value.SourceId == null, "Paired source/property selector uses staged Apply/Cancel");
        }
        void VerifyMissingWorkspaceRecovery()
        {
            const string id = "urakka.foundation.uninstalled-workspace-node";
            var guid = Guid.NewGuid().ToString();
            var json = new JsonObject
            {
                { "Guid", guid }, { "ElementType", id }, { "Position", new JsonArray { 128, 256 } },
                { "Inputs", new JsonArray { new JsonObject { { "Position", -32 }, { "BufferedItems", new JsonArray { "uninstalled_mod_item" } }, { "PrivatePortValue", "hello" } } } },
                { "Outputs", new JsonArray { new JsonObject { { "Position", 32 }, { "BufferedItems", new JsonArray() } } } },
                { "Upgrades", new JsonArray { new JsonObject { { "Type", "UninstalledModUpgrade" }, { "PrivateUpgradeValue", 42 } } } },
                { "ModSpecific", new JsonObject { { "Nested", "preserved" } } }
            };
            var replacement = FlowElement.FromJson(json, new FlowDiagram());
            Check(replacement is MissingWorkspaceElement && replacement.ElementType == id && replacement.Guid == guid &&
                replacement.Inputs.Count == 1 && replacement.Outputs.Count == 1, "Unavailable workspace type loads as inert identity-preserving placeholder");
            var saved = replacement.ToJson();
            Check(saved["ModSpecific"]["Nested"].AsString == "preserved" &&
                saved["Inputs"][0]["PrivatePortValue"].AsString == "hello" &&
                saved["Inputs"][0]["BufferedItems"][0].AsString == "uninstalled_mod_item" &&
                saved["Upgrades"][0]["PrivateUpgradeValue"].AsInteger == 42,
                "Unavailable mod upgrades, inventory and private port JSON survive opaque roundtrip");
            Check(replacement.Inputs[0].BufferSize == 0 && replacement.Inputs[0].BufferedItems.Count == 0,
                "Unavailable node cannot consume or accept native inventory while its mod is missing");
        }
    }
}
