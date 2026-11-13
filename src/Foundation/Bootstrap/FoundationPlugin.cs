using System;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Util;
using KingdomFoundation.Api;
using LightJson;
using Assets.Source.Player;
using BepInEx.Configuration;
using KingdomFoundation.Machines;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using BepInEx;
using HarmonyLib;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.Shared;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Assets.Source.Flow.Elements;
using UnityEngine.SceneManagement;
using BepInEx.Bootstrap;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Items;
using UnityEngine.InputSystem.LowLevel;

namespace KingdomFoundation
{
    [BepInPlugin(Id, "Foundation", Version)]
    public sealed class FoundationPlugin : BaseUnityPlugin
    {
        void VerifyFoundationCore()
        {
            var coreId = Core.FoundationCorePlugin.Id;
            Check(Chainloader.PluginInfos.ContainsKey(coreId), "Foundation Core companion remains loaded for compatibility");
            Check(Core.FoundationCoreApi.Core.Plugins.Has(coreId), "Core remains discoverable through the SDK plugin catalog");
            Check(Chainloader.PluginInfos[coreId].Metadata.Version == new Version(Version), "Core companion reports the unified Foundation release version");
            NexusUpdateChecker.RefreshInstalledList();
            Check(!NexusUpdateChecker.CachedMods.Any(mod => string.Equals(mod.Guid, coreId, StringComparison.OrdinalIgnoreCase)), "Installed list and update targets omit the internal Core companion");
            Check(NexusUpdateChecker.CachedMods.Count(mod => mod.Guid == Id) == 1, "Installed list contains one Foundation entry");
            Check(NexusUpdateChecker.CachedMods.Count == Chainloader.PluginInfos.Values.Count(plugin => plugin?.Metadata != null) - 1, "Installed list retains every other loaded plugin");
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
            firstListener.Dispose();
            secondListener.Dispose();
            // Signal registry: named sources and exposed property IDs must round-trip.
            var signals = new SignalRegistry();
            int state = 4, eventsSeen = 0;
            var source = signals.Register(new SignalSourceDefinition(owner, owner + ".example", "Example", new[] { new SignalPropertyDefinition("counter", "Counter", typeof(int), () => state, value => state = (int)value), new SignalPropertyDefinition("enabled", "Enabled", typeof(bool), () => true) }));
            Check(signals.TryGet(source.Id, out var found) && found.Properties.Count == 2, "Signal source and exposed properties are discoverable");
            Check(source.TryGetProperty("counter", out var value) && (int)value.Read() == 4 && value.CanWrite, "Signal property reads current data");
            value.Changed += _ => eventsSeen++;
            value.Write(5);
            Check(state == 5 && eventsSeen == 1, "Writable signal publishes one change notification");
            Check(source.TryGetProperty("enabled", out var readOnly) && !readOnly.CanWrite, "Read-only property does not expose a setter");
            Throws<InvalidOperationException>(() => readOnly.Write(true), "Read-only signal writes are rejected");
            Throws<ArgumentException>(() => signals.Register(new SignalSourceDefinition(owner, source.Id, "Duplicate", new SignalPropertyDefinition[0])), "Duplicate signal IDs are rejected");
            signals.Poll();
            state = 9;
            signals.Poll();
            Check(eventsSeen == 2, "Externally changed signal getter is detected by sampling");
            source.Dispose();
            Check(signals.Sources.Count == 0 && !signals.TryGet(owner + ".example", out _), "Disposed signal source is undiscoverable");
            // Per-kingdom data retains unknown mod entries and supports versioned callbacks.
            var saves = new ModSaveRegistry();
            int persisted = 17, restored = -1, loadedSchema = -1;
            var handle = saves.Register(owner, 2, () => new JsonObject { { "Count", persisted } }, (schema, json) =>
            {
                loadedSchema = schema;
                restored = schema > 0 ? json["Count"].AsInteger : -1;
            });
            var jsonSave = new JsonObject();
            saves.WriteState(jsonSave);
            Check(jsonSave[ModSaveRegistry.RootKey].IsJsonObject && jsonSave[ModSaveRegistry.RootKey][owner]["Version"].AsInteger == 2, "Per-kingdom mod data serialized with a schema version");
            var extra = jsonSave[ModSaveRegistry.RootKey].AsJsonObject;
            extra["some.uninstalled.mod"] = new JsonObject
            {
                {
                    "Version",
                    7
                },
                {
                    "Data",
                    new JsonObject
                    {
                        {
                            "Unknown",
                            23
                        }
                    }
                }
            };
            saves.LoadState(jsonSave);
            Check(loadedSchema == 2 && restored == 17 && saves.RetainedModIds.Count == 2, "Saved data loaded and unknown mod data retained");
            var roundTrip = new JsonObject();
            saves.WriteState(roundTrip);
            Check(roundTrip[ModSaveRegistry.RootKey]["some.uninstalled.mod"]["Data"]["Unknown"].AsInteger == 23, "Unknown mod data survives a subsequent save");
            // A provider that cannot restore data must never overwrite it with its
            // default state on the next autosave. Preserve opaque future formats too.
            const string fragileOwner = "urakka.foundation.test-loadfailure";
            int expectedMigrationFailures = 0;
            var fragile = new ModSaveRegistry(_ => expectedMigrationFailures++);
            fragile.Register(fragileOwner, 2, () => new JsonObject { { "Count", -999 } }, (schema, value) =>
            {
                if (schema > 0)
                    throw new InvalidOperationException("Simulated migration failure");
            });
            var previous = new JsonObject
            {
                {
                    ModSaveRegistry.RootKey,
                    new JsonObject
                    {
                        {
                            fragileOwner,
                            new JsonObject
                            {
                                {
                                    "Version",
                                    2
                                },
                                {
                                    "Data",
                                    new JsonObject
                                    {
                                        {
                                            "Count",
                                            12345
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "unavailable.future.mod",
                            new JsonObject
                            {
                                {
                                    "Format",
                                    "experimental"
                                },
                                {
                                    "Blob",
                                    "do not drop"
                                }
                            }
                        }
                    }
                }
            };
            fragile.LoadState(previous);
            var protectedRoundTrip = new JsonObject();
            fragile.WriteState(protectedRoundTrip);
            Check(protectedRoundTrip[ModSaveRegistry.RootKey][fragileOwner]["Data"]["Count"].AsInteger == 12345 && expectedMigrationFailures == 1, "Failed mod save migration is reported once and retains original data on subsequent saves");
            Check(protectedRoundTrip[ModSaveRegistry.RootKey]["unavailable.future.mod"]["Blob"].AsString == "do not drop", "Unrecognized future mod save format survives without interpretation");
            // If a future version changes the entire FoundationModData container,
            // preserve it instead of replacing it with our older format.
            var futureRoot = new JsonObject
            {
                {
                    ModSaveRegistry.RootKey,
                    new JsonArray
                    {
                        "future-root-format",
                        99
                    }
                }
            };
            fragile.LoadState(futureRoot);
            var rootRoundTrip = new JsonObject();
            fragile.WriteState(rootRoundTrip);
            Check(rootRoundTrip[ModSaveRegistry.RootKey].IsJsonArray && rootRoundTrip[ModSaveRegistry.RootKey][0].AsString == "future-root-format", "Unrecognized future FoundationModData root survives a save roundtrip");
            // Late provider registration must not clobber the unknown payload either.
            var lateProvider = new ModSaveRegistry();
            lateProvider.LoadState(previous);
            lateProvider.Register("unavailable.future.mod", 1, () => new JsonObject { { "Blob", "lost" } }, (schema, value) =>
            {
            });
            var lateRoundTrip = new JsonObject();
            lateProvider.WriteState(lateRoundTrip);
            Check(lateRoundTrip[ModSaveRegistry.RootKey]["unavailable.future.mod"]["Blob"].AsString == "do not drop", "Late registration cannot overwrite an unrecognized save entry");
            handle.Dispose();
            saves.NewKingdom();
            Check(saves.RetainedModIds.Count == 0, "Creating a new kingdom clears prior saved mod data");
            // Native GamePlayer.FromJson discards unknown Tech IDs. Foundation must
            // carry them forward in the normal Player.Tech array without unlocking.
            var oldUnknown = MissingResearchRecovery.RetainedIds;
            try
            {
                const string missingResearch = "urakka.foundation.tests.uninstalled-research";
                var missingDoc = new JsonObject
                {
                    {
                        "Player",
                        new JsonObject
                        {
                            {
                                "Tech",
                                new JsonArray
                                {
                                    "mine",
                                    missingResearch,
                                    missingResearch
                                }
                            },
                            {
                                "NewTechCount",
                                new JsonObject
                                {
                                    {
                                        "mine",
                                        5
                                    },
                                    {
                                        missingResearch,
                                        9
                                    }
                                }
                            }
                        }
                    }
                };
                var missingIds = MissingResearchRecovery.Inspect(missingDoc);
                Check(missingIds.Count == 1 && missingIds.Contains(missingResearch), "Research recovery discovers only unavailable, unique technology IDs");
                global::KingdomFoundation.Api.LoadSnapshot loadSnapshot = MissingResearchRecovery.PrepareLoad(missingDoc);
                Check(loadSnapshot.MissingNotifications.Count == 1 && !missingDoc["Player"]["NewTechCount"].AsJsonObject.ContainsKey(missingResearch), "Unknown native research counters are stripped before vanilla GamePlayer.FromJson");
                MissingResearchRecovery.RestoreInputOnFailure(missingDoc, loadSnapshot);
                Check(missingDoc["Player"]["NewTechCount"][missingResearch].AsInteger == 9, "Failed kingdom load restores its in-memory research counters");
                MissingResearchRecovery.PrepareLoad(missingDoc);
                missingDoc["Player"].AsJsonObject["Tech"] = new JsonArray
                {
                    "mine"
                };
                MissingResearchRecovery.Loaded(loadSnapshot);
                MissingResearchRecovery.Extend(missingDoc);
                MissingResearchRecovery.Extend(missingDoc);
                Check(missingDoc["Player"]["Tech"].AsJsonArray.Count == 2 && missingDoc["Player"]["Tech"].AsJsonArray.Any(v => v.IsString && v.AsString == missingResearch), "Missing research survives repeated saves without duplicating IDs");
                Check(missingDoc["Player"]["NewTechCount"][missingResearch].AsInteger == 9 && missingDoc["Player"]["NewTechCount"]["mine"].AsInteger == 5, "Unavailable research counters preserved without overwriting native counters");
            }
            finally
            {
                MissingResearchRecovery.Loaded(oldUnknown);
            }

            if (Assets.Source.Player.GamePlayer.Current != null && Assets.Source.Player.GamePlayer.Current.Flows.Count > 0)
            {
                // Game 1.0.14 removed the native checksum. Extended documents must
                // keep that native shape while preserving mod data and research.
                var stateDocument = SaveGame.SaveCurrentState();
                Check(!stateDocument.ContainsKey("Secret") && stateDocument["Player"].IsJsonObject, "Extended native save retains the game 1.0.14 document shape without Secret");
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
                {
                    "Guid",
                    guid
                },
                {
                    "ElementType",
                    id
                },
                {
                    "Position",
                    new JsonArray
                    {
                        128,
                        256
                    }
                },
                {
                    "Inputs",
                    new JsonArray
                    {
                        new JsonObject
                        {
                            {
                                "Position",
                                -32
                            },
                            {
                                "BufferedItems",
                                new JsonArray
                                {
                                    "uninstalled_mod_item"
                                }
                            },
                            {
                                "PrivatePortValue",
                                "hello"
                            }
                        }
                    }
                },
                {
                    "Outputs",
                    new JsonArray
                    {
                        new JsonObject
                        {
                            {
                                "Position",
                                32
                            },
                            {
                                "BufferedItems",
                                new JsonArray()
                            }
                        }
                    }
                },
                {
                    "Upgrades",
                    new JsonArray
                    {
                        new JsonObject
                        {
                            {
                                "Type",
                                "UninstalledModUpgrade"
                            },
                            {
                                "PrivateUpgradeValue",
                                42
                            }
                        }
                    }
                },
                {
                    "ModSpecific",
                    new JsonObject
                    {
                        {
                            "Nested",
                            "preserved"
                        }
                    }
                }
            };
            var replacement = FlowElement.FromJson(json, new FlowDiagram());
            Check(replacement is MissingWorkspaceElement && replacement.ElementType == id && replacement.Guid == guid && replacement.Inputs.Count == 1 && replacement.Outputs.Count == 1, "Unavailable workspace type loads as inert identity-preserving placeholder");
            var saved = replacement.ToJson();
            Check(saved["ModSpecific"]["Nested"].AsString == "preserved" && saved["Inputs"][0]["PrivatePortValue"].AsString == "hello" && saved["Inputs"][0]["BufferedItems"][0].AsString == "uninstalled_mod_item" && saved["Upgrades"][0]["PrivateUpgradeValue"].AsInteger == 42, "Unavailable mod upgrades, inventory and private port JSON survive opaque roundtrip");
            Check(replacement.Inputs[0].BufferSize == 0 && replacement.Inputs[0].BufferedItems.Count == 0, "Unavailable node cannot consume or accept native inventory while its mod is missing");
        }

        ConfigEntry<bool> enableDeveloperConsole;
        bool consoleOpen;
        Rect consoleWindow = new Rect(35, 35, 720, 590);
        Vector2 consoleScroll;
        int consoleTab;
        int selectedElement;
        float nextSignalPoll;
        private static readonly string[] ConsoleTabs =
        {
            "Overview",
            "Workspace",
            "Signals",
            "Events",
            "Diagnostics"
        };
        private void SetupDeveloperConsole()
        {
            enableDeveloperConsole = Config.Bind("Developer", "EnableConsole", false, "Enable the in-game Foundation inspector. Press F8 to show/hide. Disabled by default.");
        }

        void Update()
        {
            FoundationApi.RuntimeInternal.Tick(Time.unscaledDeltaTime);
            MachineRuntime.Tick(Time.unscaledTime);
            // Signal getters are sampled on the game thread, never from worker threads.
            if (Time.unscaledTime >= nextSignalPoll)
            {
                nextSignalPoll = Time.unscaledTime + .2f;
                FoundationApi.SignalsInternal.Poll();
            }

            if (enableDeveloperConsole?.Value == true && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                consoleOpen = !consoleOpen;
                // A single event per keystroke lets the full-suite verifier
                // distinguish an input/config failure from an IMGUI visibility
                // failure. No per-frame log output and no new gameplay hooks.
                Logger.LogInfo("[FOUNDATION CONSOLE] F8 toggled " + (consoleOpen ? "OPEN" : "CLOSED"));
            }
        }

        void OnGUI()
        {
            if (enableDeveloperConsole?.Value != true || !consoleOpen)
                return;
            consoleWindow = GUI.Window(765142, consoleWindow, DrawDeveloperWindow, "Foundation Developer Inspector [F8]");
        }

        void DrawDeveloperWindow(int id)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < ConsoleTabs.Length; i++)
                if (GUILayout.Toggle(consoleTab == i, ConsoleTabs[i], GUI.skin.button))
                {
                    if (consoleTab != i)
                        consoleScroll = Vector2.zero;
                    consoleTab = i;
                }

            if (GUILayout.Button("X", GUILayout.Width(26)))
                consoleOpen = false;
            GUILayout.EndHorizontal();
            consoleScroll = GUILayout.BeginScrollView(consoleScroll);
            try
            {
                switch (consoleTab)
                {
                    case 0:
                        DrawOverview();
                        break;
                    case 1:
                        DrawWorkspace();
                        break;
                    case 2:
                        DrawSignals();
                        break;
                    case 3:
                        DrawEvents();
                        break;
                    case 4:
                        DrawDiagnostics();
                        break;
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label("Inspector error: " + ex.Message);
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, consoleWindow.width, 22));
        }

        void DrawOverview()
        {
            GUILayout.Label("Framework " + Version);
            var core = KingdomFoundation.Core.FoundationCoreApi.Core;
            GUILayout.Label("Core API: " + core.ApiVersion);
            GUILayout.Label("Discovered mods: " + core.Mods.Entries.Count);
            GUILayout.Label("Core content registrations: " + core.Content.Registered.Count);
            GUILayout.Label("Native input/output values are inspected through FoundationApi.Flow and FoundationApi.IO.");
            GUILayout.Label("Research nodes: " + FoundationApi.TechNodes.Nodes.Count);
            GUILayout.Label("Workspace types: " + FoundationApi.WorkspaceNodes.Nodes.Count);
            GUILayout.Label("Mod bar items: " + FoundationApi.ModBar.Items.Count);
            GUILayout.Label("Settings sections: " + FoundationApi.ModConfig.Sections.Count);
            GUILayout.Label("Signal sources: " + FoundationApi.Signals.Sources.Count);
            GUILayout.Label("Event subscriptions: " + FoundationApi.Events.Subscriptions.Count);
            GUILayout.Label("Retained save owners: " + string.Join(", ", FoundationApi.ModSave.RetainedModIds));
            GUILayout.Label("Unavailable research IDs retained: " + FoundationApi.ResearchRecovery.RetainedTechIds.Count);
            GUILayout.Space(12);
            GUILayout.Label("Native modules: World / Machines / Flow / Economy / Production / Research / Adventure / Combat");
            GUILayout.Label("SDK: Items / Content / Upgrades / Signals / UI / Saves / Events / Assets / Runtime");
            GUILayout.Label("The native game's simulation remains authoritative. Measured IO rates are observational.");
        }

        void DrawWorkspace()
        {
            var player = FoundationApi.World.Player;
            if (player == null || player.Flows.Count == 0)
            {
                GUILayout.Label("No kingdom loaded.");
                return;
            }

            var flow = FoundationApi.World.ActiveFlow;
            GUILayout.Label("Flow: " + player.CurrentFlow + " (" + (flow.Label ?? "unnamed") + ") — " + flow.Elements.Count + " elements, " + flow.Pipes.Count + " pipes");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", GUILayout.Width(100)))
                selectedElement = Math.Max(0, selectedElement - 1);
            if (GUILayout.Button("Next", GUILayout.Width(100)))
                selectedElement = Math.Min(flow.Elements.Count - 1, selectedElement + 1);
            GUILayout.EndHorizontal();
            if (flow.Elements.Count == 0)
                return;
            selectedElement = Math.Max(0, Math.Min(selectedElement, flow.Elements.Count - 1));
            var element = flow.Elements[selectedElement];
            var snapshot = FoundationApi.Flow.Inspect(element);
            GUILayout.Label("Element [" + selectedElement + "] " + snapshot.Label + " / " + snapshot.TypeId);
            GUILayout.Label("GUID: " + snapshot.Guid);
            GUILayout.Label("Position: " + snapshot.Position + "  Size: " + snapshot.Size);
            GUILayout.Label("Progress: " + snapshot.Progress.ToString("0.000") + "  Stack: " + snapshot.StackSize);
            foreach (var port in snapshot.Inputs.Concat(snapshot.Outputs))
                GUILayout.Label(" " + port.Direction + "[" + port.Index + "] item=" + port.AcceptedItemTypeId + " buffer=" + port.BufferedCount + "/" + port.BufferCapacity + " connected=" + port.IsConnected);
            foreach (var pipe in flow.Pipes.Where(p => p.ConnectedOutput?.Parent == element || p.ConnectedInput?.Parent == element))
            {
                var stats = FoundationApi.IO.GetMeasuredThroughput(pipe);
                GUILayout.Label(" Pipe " + pipe.ConnectedOutput?.Parent?.Guid + " -> " + pipe.ConnectedInput?.Parent?.Guid + "  transfers=" + stats.TotalTransfers + "  observed/s=" + stats.AverageTransfersPerSecond.ToString("0.0") + " (10s)");
            }
        }

        void DrawSignals()
        {
            foreach (var source in FoundationApi.Signals.Sources)
            {
                GUILayout.Label("SOURCE " + source.Name + " (" + source.Id + ") owner=" + source.ModId);
                foreach (var property in source.Properties)
                {
                    string value;
                    try
                    {
                        value = property.Read()?.ToString() ?? "null";
                    }
                    catch (Exception ex)
                    {
                        value = "ERROR: " + ex.Message;
                    }

                    GUILayout.Label("    " + property.Name + " [" + property.Id + "] " + property.ValueType.Name + " = " + value + (property.CanWrite ? " (RW)" : " (RO)"));
                }
            }

            if (FoundationApi.Signals.Sources.Count == 0)
                GUILayout.Label("No mod has registered a signal source.");
        }

        void DrawEvents()
        {
            foreach (var subscription in FoundationApi.Events.Subscriptions.OrderBy(e => e.EventType.Name).ThenBy(e => e.ModId))
                GUILayout.Label(subscription.EventType.Name + " <= " + subscription.ModId);
        }

        void DrawDiagnostics()
        {
            foreach (var entry in FoundationApi.Diagnostics.Recent.Reverse())
                GUILayout.Label(entry.TimestampUtc.ToString("HH:mm:ss") + " [" + entry.Level + "] " + entry.Owner + ": " + entry.Message);
        }

        public const string Id = "urakka.kingdominc.foundation", Version = "0.14.0";
        internal static FoundationPlugin Instance;
        public static BepInEx.Configuration.ConfigEntry<string> NexusApiKeyConfig;
        internal Harmony hooks;
        internal static bool Diagnostic => Environment.GetCommandLineArgs().Any(arg => arg == "-foundation-smoke" || arg == "-foundation-options-smoke");

        public static string GetNexusApiKey()
        {
            string key = NexusApiKeyConfig?.Value;
            if (!string.IsNullOrEmpty(key))
                return key.Trim();
            key = Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.Process) ?? Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.User) ?? Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrEmpty(key))
                return key.Trim();
            // Never distribute an API credential inside a public mod DLL.
            return null;
        }

        void Awake()
        {
            Instance = this;
            SetupDeveloperConsole();
            NexusApiKeyConfig = Config.Bind("Updates", "NexusApiKey", "", "Optional override for Nexus Mods API Key.");
            Assets.Source.Util.Translation.Init();
            hooks = new Harmony(Id);
            FoundationApi.GameHooksInternal.Install(hooks);
            foreach (var type in new[]
            {
                typeof(global::KingdomFoundation.OpenTree),
                typeof(global::KingdomFoundation.CloseTree),
                typeof(global::KingdomFoundation.ShowResearch),
                typeof(global::KingdomFoundation.RefreshResearch),
                typeof(global::KingdomFoundation.FocusResearch),
                typeof(global::KingdomFoundation.RequirementNavigation),
                typeof(global::KingdomFoundation.ListResearch),
                typeof(global::KingdomFoundation.TreeUpdate),
                typeof(global::KingdomFoundation.EmptyBounds),
                typeof(global::KingdomFoundation.CustomIcon),
                typeof(global::KingdomFoundation.FixedCosts),
                typeof(global::KingdomFoundation.SaveNotifications),
                typeof(global::KingdomFoundation.ReadResearch)
            }

            )
                hooks.CreateClassProcessor(type).Patch();
            WorkspaceIntegration.Install(hooks);
            FlowIntegration.Install(hooks);
            SaveIntegration.Install(hooks);
            WorldEventIntegration.Install(hooks);
            GameEventIntegration.Install(hooks);
            UpgradeIntegration.Install(hooks);
            ItemIntegration.Install(hooks);
            GameplayIntegration.Install(hooks);
            FoundationNativeSignals.Install();
            // Machine is an in-process Foundation module, not a second plugin.
            MachineRuntime.Initialize(hooks, Logger);
            FoundationNativeMachinePanels.Install();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.CreateModBar)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.EscapeModBar)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.HideModBar)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIStartPatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIOnEnablePatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIApplyPatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUICancelPatch)).Patch();
            if (Diagnostic)
            {
                // GameRoot can be isolated, but Unity's persistentDataPath remains
                // shared by all copies run under the same Windows user. Redirect
                // AND block native writes before the game scene starts.
                hooks.CreateClassProcessor(typeof(global::KingdomFoundation.BlockExistingSaveInSmoke)).Patch();
                hooks.CreateClassProcessor(typeof(global::KingdomFoundation.BlockAnyNativeSaveInSmoke)).Patch();
                IsolateSmokeSaves();
                InstallDiagnostics();
                Application.runInBackground = true;
                StartCoroutine(Environment.GetCommandLineArgs().Contains("-foundation-options-smoke") ? new OptionsMenuChecks().Run() : RuntimeCheck());
            }

            Logger.LogInfo("Foundation " + Version + ": native game services, content, quests, dungeons, wars, economy, IO, UI, events and SDK ready.");
        }

        // Unity's persistentDataPath is shared even if the game binary is copied
        // into a temporary sandbox. It is not safe to merely prevent reading saves:
        // GameManager.OnApplicationQuit always calls StoreAutosaveState, which would
        // otherwise overwrite the user's real autosave-exit.save.
        static void IsolateSmokeSaves()
        {
            string isolated = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification", "IsolatedSaves");
            Directory.CreateDirectory(isolated);
            SaveGame.SavesPath = isolated;
            AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, new DirectoryInfo(isolated));
            AccessTools.Field(typeof(SaveGame), "_saves").SetValue(null, null);
            UnityEngine.Debug.LogWarning("Foundation smoke mode: game saves redirected to " + isolated + " (native disk saves disabled).");
        }

        internal static TechTreeTabs Tabs(TechTreeMenu menu) => menu ? menu.GetComponent<TechTreeTabs>() : null;
        internal static bool ModsOpen => TechTreeMenu.Visible && Tabs(TechTreeMenu.Instance)?.ModsSelected == true;

        internal static IEnumerable<TechNode> VisibleResearch() => TechNode.Nodes.Where(n => ModsOpen ? NativeResearch.Active(n) && NativeResearch.ModOf(n)?.Id == Tabs(TechTreeMenu.Instance)?.SelectedModId : !NativeResearch.Owns(n));
        internal static IEnumerable<CodeInstruction> FilterCatalog(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(TechNode), nameof(TechNode.Nodes));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(FoundationPlugin), nameof(VisibleResearch));
                }

                yield return instruction;
            }
        }

        void OnDestroy()
        {
            FoundationApi.AudioInterceptionInternal.Shutdown();
            FoundationNativeMachinePanels.Uninstall();
            FoundationPanelHost.ResetAll();
            ((FoundationPanelRegistry)FoundationApi.Panels).Clear();
            MachineRuntime.Shutdown();
            WorkspaceIntegration.Remove();
            foreach (var bar in Resources.FindObjectsOfTypeAll<ModBarHost>())
                Destroy(bar);
            foreach (var links in Resources.FindObjectsOfTypeAll<RequirementLinks>())
                Destroy(links);
            foreach (var tabs in Resources.FindObjectsOfTypeAll<TechTreeTabs>())
                Destroy(tabs);
            foreach (var menu in Resources.FindObjectsOfTypeAll<FoundationConfigMenu>())
                Destroy(menu);
            hooks?.UnpatchSelf();
        }

        IModBarRegistration lifecycleItem;
        global::KingdomFoundation.ProbeBarElement lifecycleElement;
        ModBarHost testedBar;
        void VerifyModBar(string directory)
        {
            var currentPlayer = Assets.Source.Player.GamePlayer.Current;
            Assets.Source.Player.GamePlayer snapshotPlayer;
            try
            {
                snapshotPlayer = Assets.Source.Player.GamePlayer.FromJson(currentPlayer.ToJson());
            }
            finally
            {
                Assets.Source.Player.GamePlayer.Current = currentPlayer;
            }

            using (var lateResearch = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.cached-player", "Snapshot storage")))
                Check(!snapshotPlayer.HasTech(((INativeTechNode)lateResearch).NativeNode), "Player snapshot created before late research safely reports its unpurchased state");
            var registry = new ModBarRegistry();
            var a = registry.Register(new ModBarText("mod.a", "same", "A", order: 2, alignment: ModBarAlignment.Right));
            var snapshot = registry.Items;
            var b = registry.Register(new ModBarText("mod.b", "same", "B", order: -1, alignment: ModBarAlignment.Left));
            var c = registry.RegisterSeparator("mod.c", "sep", ModBarAlignment.Center, order: 5);
            Check(registry.Items[0] == b && registry.Items[1] == c && registry.Items[2] == a && snapshot.Count == 1, "Mod Bar registry sorts controls by Alignment then Order and returns stable snapshots");
            Throws<ArgumentException>(() => registry.Register(new ModBarText("mod.a", "same", "Duplicate")), "Mod Bar duplicate identity rejected within a mod");
            Check(registry.TryGet("mod.b", "same", out var found) && found == b, "Different mods may use the same local Mod Bar item ID");
            Throws<ArgumentOutOfRangeException>(() => new ModBarText("mod.a", "bad", "", float.NaN), "Invalid Mod Bar width rejected");
            Throws<ArgumentOutOfRangeException>(() => new ModBarText("mod.a", "bad", "", 180, 0, (ModBarAlignment)99), "Invalid Mod Bar alignment rejected");
            Throws<ArgumentException>(() => new ModBarButton("", "button", ""), "Mod Bar requires an owning mod GUID");
            Throws<ArgumentException>(() => new ModBarDropdown("mod.a", "dropdown", Array.Empty<string>()), "Empty dropdown rejected");
            Throws<ArgumentOutOfRangeException>(() => new ModBarDropdown("mod.a", "dropdown", new[] { "One" }, 1), "Out of range dropdown selection rejected");
            a.Dispose();
            a.Dispose();
            var replacement = registry.Register(new ModBarText("mod.a", "same", "Replacement"));
            a.Dispose();
            Check(replacement.IsRegistered && registry.Items.Count == 3, "Stale Mod Bar disposal cannot remove a replacement registration");
            Throws<ObjectDisposedException>(a.Refresh, "Disposed Mod Bar handle rejects refresh");
            b.Dispose();
            c.Dispose();
            replacement.Dispose();
            var pane = GameUI.Instance.GetComponentInChildren<CurrencyPane>(true);
            testedBar = pane.GetComponent<ModBarHost>();
            Check(testedBar && testedBar.Clip && testedBar.Bar && testedBar.Clip.parent == pane.transform.parent && testedBar.Bar.parent == testedBar.Clip, "Shared Mod Bar is created beneath the native currency HUD inside its slide clip container");
            Check(!testedBar.Bar.Find("Heading") && !testedBar.Bar.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "Mod Bar"), "Mod Bar omits the static Mod Bar heading label");
            var currency = (RectTransform)pane.transform;
            var parent = (RectTransform)currency.parent;
            var originalCurrency = currency.anchoredPosition;
            var corners = new Vector3[4];
            currency.GetWorldCorners(corners);
            float bottom = parent.InverseTransformPoint(corners[0]).y;
            Check(Mathf.Approximately(testedBar.Clip.anchoredPosition.y, bottom - parent.rect.yMax - ModBarHost.Gap) && Mathf.Approximately(ModBarHost.Gap, 2f) && currency.anchoredPosition == originalCurrency, "Mod Bar leaves a 2-pixel gap beneath the vanilla currency bar without moving native currency UI");
            Check(testedBar.ToggleButton && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "^" && ((RectTransform)testedBar.ToggleButton.transform.parent).anchorMin == new Vector2(.5f, 1), "Mod Bar includes a top-center chevron visibility toggle");
            ModBarHost.Attach(GameUI.Instance);
            Check(pane.GetComponents<ModBarHost>().Length == 1, "Repeated HUD attachment does not duplicate the Mod Bar");
            Check(testedBar.Bar.GetComponent<Image>().sprite == pane.GetComponent<Image>().sprite, "Mod Bar reuses the native currency frame sprite");
            Check(FoundationApi.ModBar.Items.Count >= 6 && testedBar.Slots.Count >= 6, "Separately compiled consumer registers text, button, separator, dropdown, toggle and custom interface implementation before scene load");
            var sepSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:separator");
            Check(sepSlot.Find("Separator") && sepSlot.Find("Separator").GetComponent<Image>() && !sepSlot.Find("Separator").GetComponent<Image>().raycastTarget, "ModBarSeparator creates a non-raycasting vertical divider inside its slot");
            var leftSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:status");
            var centerSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:enabled");
            var rightSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:custom");
            Check(leftSlot.anchoredPosition.x < centerSlot.anchoredPosition.x && centerSlot.anchoredPosition.x < rightSlot.anchoredPosition.x, "Mod Bar places Left, Center and Right aligned controls across their respective regions");
            var exampleButton = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:button").GetComponentInChildren<Button>();
            var status = leftSlot.GetComponentInChildren<TMP_Text>();
            var gold = Assets.Source.Player.GamePlayer.Current.GetCurrency(Assets.Source.Player.Currency.Gold);
            Click(exampleButton);
            testedBar.Refresh();
            Check(status.text == "Clicks: 1" && Assets.Source.Player.GamePlayer.Current.GetCurrency(Assets.Source.Player.Currency.Gold) == gold, "Mod Bar button invokes external callback without changing currency");
            var definition = (ModBarButton)FoundationApi.ModBar.Items.Single(r => r.Id == "button" && r.ModId == "urakka.foundation.example").Element;
            definition.Interactable = false;
            FoundationApi.ModBar.Items.Single(r => ReferenceEquals(r.Element, definition)).Refresh();
            testedBar.Refresh();
            ExecuteEvents.Execute(exampleButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Check(status.text == "Clicks: 1" && !exampleButton.interactable, "Disabled Mod Bar button ignores pointer clicks");
            definition.Interactable = true;
            testedBar.Refresh();
            var dropdown = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:period").GetComponentInChildren<TMP_Dropdown>();
            Check(dropdown.GetComponent<Image>().sprite == GameUI.Instance.AccountantPopup.GetComponentInChildren<TMP_Dropdown>(true).GetComponent<Image>().sprite, "Mod Bar dropdown uses the game's native control prefab");
            dropdown.Show();
            Canvas.ForceUpdateCanvases();
            var list = dropdown.transform.Find("Dropdown List");
            Check(list && list.gameObject.activeInHierarchy, "Native Mod Bar dropdown opens its options");
            Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-dropdown.png"));
            File.WriteAllLines(Path.Combine(directory, "mod-bar-dropdown-layout.txt"), list.GetComponentsInChildren<RectTransform>(true).Select(r => r.name + " rect=" + r.rect + " world=" + r.position));
            var option = list.GetComponentsInChildren<Toggle>().Last();
            PointerClick(option);
            testedBar.Refresh();
            var dropdownElement = (ModBarDropdown)FoundationApi.ModBar.Items.Single(r => r.Id == "period" && r.ModId == "urakka.foundation.example").Element;
            Check(dropdownElement.SelectedIndex == 2 && status.text == "Period: Hour", "Native option selection updates typed dropdown state and external callback");
            dropdownElement.SetSelectedIndex(1);
            testedBar.Refresh();
            Check(dropdown.value == 1 && status.text == "Period: Hour", "Programmatic dropdown refresh does not re-fire selection callbacks");
            dropdown.Hide();
            PointerClick(testedBar.ToggleButton);
            testedBar.StepSlide(0.05f);
            Check(!testedBar.IsExpanded && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "v" && testedBar.SlideProgress > 0f && testedBar.SlideProgress < 1f && testedBar.Bar.anchoredPosition.y > 0f && testedBar.Bar.anchoredPosition.y < ModBarHost.Height, "Clicking the top-center chevron starts a smooth non-instant upward slide transition");
        }

        void FinishModBarChecks(string directory)
        {
            testedBar.StepSlide(ModBarHost.SlideDuration);
            Check(Mathf.Approximately(testedBar.SlideProgress, 0f) && Mathf.Approximately(testedBar.Bar.anchoredPosition.y, ModBarHost.Height) && testedBar.ToggleButton.gameObject.activeInHierarchy, "Collapsed Mod Bar finishes sliding out of view while keeping the top-center chevron button accessible");
            PointerClick(testedBar.ToggleButton);
            testedBar.StepSlide(0.05f);
            Check(testedBar.IsExpanded && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "^" && testedBar.SlideProgress > 0f && testedBar.SlideProgress < 1f, "Clicking the chevron again slides the Mod Bar smoothly back into view");
            testedBar.StepSlide(ModBarHost.SlideDuration);
            Check(Mathf.Approximately(testedBar.SlideProgress, 1f) && Mathf.Approximately(testedBar.Bar.anchoredPosition.y, 0f), "Expanded Mod Bar settles at its full open position");
            var pane = GameUI.Instance.GetComponentInChildren<CurrencyPane>(true);
            var status = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:status").GetComponentInChildren<TMP_Text>();
            var toggle = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:enabled").GetComponentInChildren<Toggle>();
            PointerClick(toggle);
            testedBar.Refresh();
            Check(toggle.isOn && status.text == "Enabled", "Native toggle invokes the mod-owned bool callback");
            var hostCanvas = pane.GetComponentInParent<Canvas>();
            GameUI.Instance.ToggleUI(false);
            Check(!hostCanvas.enabled && testedBar.Bar.GetComponentInParent<Canvas>() == hostCanvas, "Mod Bar obeys native HUD visibility and UI scale");
            GameUI.Instance.ToggleUI(true);
            pane.gameObject.SetActive(false);
            Check(!testedBar.Bar.gameObject.activeSelf && !testedBar.Clip.gameObject.activeSelf, "Disabling the currency HUD also disables its sibling Mod Bar");
            pane.gameObject.SetActive(true);
            testedBar.Refresh();
            Check(testedBar.Bar.gameObject.activeInHierarchy, "Re-enabling the currency HUD restores one Mod Bar and its controls");
            var blocker = testedBar.Bar.GetComponent<UIBlocksZoom>();
            blocker.OnPointerEnter(new PointerEventData(EventSystem.current));
            Check(UIBlocksZoom.Blocked, "Mod Bar controls block workspace mouse-wheel zoom while hovered");
            blocker.OnPointerExit(new PointerEventData(EventSystem.current));
            var textDefinition = (ModBarText)FoundationApi.ModBar.Items.Single(r => r.Id == "status" && r.ModId == "urakka.foundation.example").Element;
            var textHandle = FoundationApi.ModBar.Items.Single(r => ReferenceEquals(r.Element, textDefinition));
            textDefinition.IsVisible = false;
            textHandle.Refresh();
            testedBar.Refresh();
            Check(!status.gameObject.activeInHierarchy, "Polymorphic visibility removes hidden controls from Mod Bar layout");
            textDefinition.IsVisible = true;
            textDefinition.Text = "Example ready";
            textHandle.Refresh();
            testedBar.Refresh();
            lifecycleElement = new global::KingdomFoundation.ProbeBarElement();
            lifecycleItem = FoundationApi.ModBar.Register(lifecycleElement);
            testedBar.Refresh();
            Check(lifecycleElement.Creates == 1 && lifecycleElement.Refreshes > 0, "Late custom IModBarElement creates and refreshes its own IModBarView");
            Check(lifecycleElement.InitialSize == new Vector2(180, 40), "Custom view receives its allocated slot dimensions during construction");
            var temporary = FoundationApi.ModBar.Register(new ModBarText("foundation.diagnostics", "remove", "Temporary"));
            testedBar.Refresh();
            temporary.Dispose();
            testedBar.Refresh();
            Check(!testedBar.Slots.Any(s => s.name.EndsWith(":remove")), "Disposing a registration removes its live control");
            var parent = (RectTransform)testedBar.Clip.parent;
            var oldSize = parent.sizeDelta;
            try
            {
                parent.sizeDelta = new Vector2(oldSize.x - 1000, oldSize.y);
                testedBar.Refresh();
                Check(testedBar.Scroll.content.rect.width > testedBar.Scroll.viewport.rect.width && testedBar.Scroll.horizontal && !testedBar.Scroll.vertical, "Narrow HUD uses horizontal overflow instead of overlapping controls");
                Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-overflow.png"));
                PointerClick(testedBar.Bar.Find("Next").GetComponentInChildren<Button>());
                Check(testedBar.Scroll.horizontalNormalizedPosition > 0, "Overflow arrow reaches controls beyond the visible viewport");
            }
            finally
            {
                parent.sizeDelta = oldSize;
                testedBar.Refresh();
            }

            testedBar.Scroll.horizontalNormalizedPosition = 0;
            var visibleSlots = testedBar.Slots.Where(s => s.gameObject.activeSelf).OrderBy(s => s.anchoredPosition.x).ToArray();
            Check(visibleSlots.Zip(visibleSlots.Skip(1), (left, right) => left.anchoredPosition.x + left.rect.width <= right.anchoredPosition.x).All(v => v), "Visible controls occupy separate slots in deterministic order");
            Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar.png"));
        }

        void PointerClick(Selectable control)
        {
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(((RectTransform)control.transform).rect.center));
            var data = new PointerEventData(EventSystem.current)
            {
                position = point,
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == control, "Mod Bar control receives actual UI raycasts: " + control.name + " hits=" + string.Join(",", hits.Select(h => h.gameObject.name)));
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        IEnumerator VerifyModCategories(TechTreeMenu menu, TechTreeTabs tabs, Camera camera, string directory)
        {
            const string alpha = "foundation.layout.alpha", beta = "foundation.layout.beta";
            var example = FoundationApi.TechMods.Single(m => m.Id == "urakka.foundation.example");
            Check(example.Name == "Foundation Example" && example.Nodes.Count == 3, "Existing separately compiled consumers automatically receive their BepInEx mod category");
            Check(tabs.Sidebar.Panel.gameObject.activeSelf && tabs.SelectedModId == example.Id, "Mods opens with its native-style left mod list and a selected category");
            Check(menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == example.Id), "The selected category excludes other mods' root technologies");
            Check(FoundationApi.TechMods.Sum(m => m.Nodes.Count) == FoundationApi.TechNodes.Nodes.Count, "Every active registration belongs to exactly one mod category");
            var registrations = new List<ITechNode>();
            Func<string, string, string, string, TechNodeOptions, ITechNode> add = (id, name, mod, title, rules) =>
            {
                var result = FoundationApi.CreateTechNode(new TechNodeDefinition(id, name, mod, title, research: rules));
                registrations.Add(result);
                return result;
            };
            try
            {
                var a = add("foundation.layout.a", "Alpha Root", alpha, "Layout Alpha", null);
                var b = add("foundation.layout.b", "Beta Root", beta, "Layout Beta", null);
                var sibling = add("foundation.layout.sibling", "Same Requested Position", alpha, "Layout Alpha", null);
                var dependent = add("foundation.layout.dependent", "Cross-Mod Dependency", alpha, "Layout Alpha", new TechNodeOptions(requirements: new[] { b.Id, "requisition_office" }));
                var child = add("foundation.layout.child", "Child At Parent Position", alpha, "Layout Alpha", new TechNodeOptions(nodeType: TechNodeType.Utility, rootId: a.Id, previousId: a.Id));
                add("foundation.layout.resource", "Resource At Parent Position", alpha, "Layout Alpha", new TechNodeOptions(nodeType: TechNodeType.Resource, rootId: a.Id));
                var categories = FoundationApi.TechMods;
                Check(categories.Count(m => m.Id == alpha) == 1 && categories.Single(m => m.Id == alpha).Nodes.Count == 5, "Multiple technologies register under one explicit mod category");
                Throws<ArgumentException>(() => a.Update(new TechNodeDefinition(a.Id, "Wrong owner", beta, "Layout Beta")), "Node updates cannot silently move ownership to another mod");
                Throws<ArgumentException>(() => FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.layout.invalid", "Wrong branch", alpha, "Layout Alpha", research: new TechNodeOptions(rootId: b.Id))), "Cross-mod branch parents are rejected; cross-mod prerequisites remain supported");
                var aModel = ((INativeTechNode)a).NativeNode;
                var bModel = ((INativeTechNode)b).NativeNode;
                menu.FocusNode(aModel);
                yield return null;
                Check(tabs.SelectedModId == alpha && tabs.Sidebar.Buttons.ContainsKey(alpha) && tabs.Sidebar.Buttons.ContainsKey(beta), "Late registration adds both categories to the live mod list");
                Check(menu.Nodes.Count() == 3 && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == alpha), "Selecting Alpha renders only its three root nodes");
                Check(a.Definition.Position == sibling.Definition.Position && aModel.Position != ((INativeTechNode)sibling).NativeNode.Position, "Duplicate requested coordinates are resolved automatically without changing definitions");
                CheckNoOverlap(menu, "Category root frames, costs and multi-line requirement panels do not overlap");
                var positions = menu.Nodes.ToDictionary(n => n.Tech.Identifier, n => n.Tech.Position);
                menu.UpdateTechTree();
                menu.UpdateTechTree();
                Check(menu.Nodes.All(n => positions[n.Tech.Identifier] == n.Tech.Position), "Repeated status refreshes preserve resolved positions without layout drift");
                camera.transform.position += new Vector3(350, 0, 0);
                Capture(camera, Path.Combine(directory, "foundation-mod-list.png"));
                Click(tabs.Sidebar.Buttons[beta]);
                yield return null;
                Check(tabs.SelectedModId == beta && menu.Nodes.Count() == 1 && menu.Nodes.Single().Tech == bModel, "A real sidebar button switches to Beta's independent research map");
                Check(aModel.Position == bModel.Position, "Different category pages can reuse the same coordinates without sharing a visible space");
                menu.FocusNode(((INativeTechNode)child).NativeNode);
                yield return null;
                Check(tabs.SelectedModId == alpha && menu.RootNode == aModel && menu.Nodes.Any(n => n.Tech.Identifier == child.Id), "Native focus automatically switches mod category and opens the target branch");
                CheckNoOverlap(menu, "A branch root and child at identical requested coordinates are separated");
                Check(menu.Nodes.Any(n => n.Tech.Identifier == "foundation.layout.resource"), "Native generic resource nodes participate in the same branch layout");
                var childModel = ((INativeTechNode)child).NativeNode;
                var parent = TechTreeTabs.Field<RectTransform>(menu, "_nodeParent");
                Check(parent.GetComponentsInChildren<LineRenderer>().Any(line => line.positionCount == 2 && (Vector2)line.GetPosition(0) == childModel.Position && (Vector2)line.GetPosition(1) == aModel.Position), "Native connectors follow the resolved node positions");
                CheckFocused(camera, childModel, "Focus centers the actual resolved branch node");
                WorldFlowCamera.Instance.Move(72, 48);
                var savedPosition = camera.transform.position;
                var savedSize = camera.orthographicSize;
                Click(tabs.Sidebar.Buttons[beta]);
                Click(tabs.Sidebar.Buttons[alpha]);
                Check(menu.RootNode == aModel && camera.transform.position == savedPosition && camera.orthographicSize == savedSize, "Every category retains its branch, camera position and zoom");
                menu.FocusNode(((INativeTechNode)dependent).NativeNode);
                yield return null;
                RequirementClick(Node(menu, dependent.Id), camera, 0);
                Check(tabs.SelectedModId == beta && menu.Nodes.Single().Tech == bModel && !bModel.IsPurchased, "A requirement link selects its owning mod category without purchasing research");
                menu.FocusNode(aModel);
                sibling.Update(new TechNodeDefinition(sibling.Id, "Moved Onto Root", (Vector2)aModel.Position));
                yield return null;
                CheckNoOverlap(menu, "Live definition updates cannot place a technology over another visible technology");
                for (int i = 0; i < 14; i++)
                    add("foundation.layout.scroll." + i, "Scroll Root", "foundation.layout.scrollmod." + i, "Scroll Mod " + i.ToString("00"), null);
                Check(FoundationApi.TechMods.Count == categories.Count + 14, "Category enumeration snapshots remain stable after new mod registrations");
                yield return null;
                Canvas.ForceUpdateCanvases();
                var scroll = tabs.Sidebar.Scroll;
                Check(scroll.content.rect.height > scroll.viewport.rect.height, "The mod list scrolls when installed categories exceed its height");
                var eventData = new PointerEventData(EventSystem.current)
                {
                    scrollDelta = new Vector2(0, -12)
                };
                ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.scrollHandler);
                Check(scroll.content.anchoredPosition.y > 0, "Native ScrollRect wheel handling reveals off-screen mod categories");
                var blocker = tabs.Sidebar.Panel.GetComponent<Assets.Behaviour.UI.Shared.UIBlocksZoom>();
                blocker.OnPointerEnter(eventData);
                var zoomBlockers = (HashSet<Assets.Behaviour.UI.Shared.UIBlocksZoom>)HarmonyLib.AccessTools.Field(typeof(Assets.Behaviour.UI.Shared.UIBlocksZoom), "blockingZoom").GetValue(null);
                Check(zoomBlockers.Contains(blocker), "Entering the sidebar registers its own native zoom blocker");
                var zoomBeforeScroll = camera.orthographicSize;
                Wheel(UnityEngine.InputSystem.Mouse.current, -120);
                Check(camera.orthographicSize == zoomBeforeScroll, "Mouse wheel over the mod list is blocked from zooming the research map");
                blocker.OnPointerExit(eventData);
                Check(!zoomBlockers.Contains(blocker), "Leaving the sidebar releases its native zoom blocker");
                Check(zoomBlockers.All(active => active && active.isActiveAndEnabled && active.gameObject.activeInHierarchy), "Other native zoom blockers belong to active UI views");
                scroll.verticalNormalizedPosition = 1;
                menu.FocusNode(bModel);
                b.Dispose();
                yield return null;
                Check(!tabs.Sidebar.Buttons.ContainsKey(beta) && !FoundationApi.TechMods.Any(m => m.Id == beta), "Removing the last node removes its category from the sidebar");
                Check(tabs.SelectedModId != beta && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == tabs.SelectedModId), "Removing the selected category safely selects another populated category");
                Click(tabs.ResearchButton);
                Check(!tabs.Sidebar.Panel.gameObject.activeSelf && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Base Game hides the mod list and retains its original research catalog");
            }
            finally
            {
                foreach (var node in registrations.AsEnumerable().Reverse())
                    node.Dispose();
            }

            yield return null;
            tabs.SelectMod(example.Id, false);
            menu.ShowTechTree(null, true);
        }

        void CheckNoOverlap(TechTreeMenu menu, string name)
        {
            Canvas.ForceUpdateCanvases();
            var parent = TechTreeTabs.Field<RectTransform>(menu, "_nodeParent");
            var rects = menu.Nodes.Select(n => ModResearchLayout.Bounds(n, parent)).ToArray();
            bool clear = true;
            for (int i = 0; i < rects.Length; i++)
                for (int j = i + 1; j < rects.Length; j++)
                    clear &= !rects[i].Overlaps(rects[j]);
            Check(clear, name);
        }

        void VerifyModConfig()
        {
            var registry = new ModConfigRegistry();
            Check(registry.Sections.Count == 0, "ModConfigRegistry starts empty");
            // 1. Get or create section
            var section = registry.GetOrCreateSection("test.mod", "Test Mod", "A mod for testing config");
            Check(registry.Sections.Count == 1, "Section successfully added");
            Check(section.ModId == "test.mod" && section.ModName == "Test Mod", "Section properties match");
            Check(registry.TryGetSection("test.mod", out var found) && ReferenceEquals(found, section), "TryGetSection retrieves registered section");
            // 2. Add standard elements
            int clickCount = 0;
            var btn = section.AddButton("Test Button", () => clickCount++, "Click Me");
            Check(btn.Label == "Test Button" && btn.ButtonText == "Click Me", "Button properties correct");
            btn.OnClick();
            Check(clickCount == 1, "Button click executes callback");
            string currentText = "Initial Text";
            var textEl = section.AddText(currentText, isHeader: true);
            Check(textEl.Text == currentText && textEl.IsHeader, "Text element properties correct");
            bool toggleVal = false;
            int toggleChangeCount = 0;
            var toggleEl = section.AddToggle("Test Toggle", false, val =>
            {
                toggleVal = val;
                toggleChangeCount++;
            }, () => toggleVal);
            Check(!toggleEl.Value, "Toggle starts false");
            float sliderVal = 10f;
            var sliderEl = section.AddSlider("Test Slider", 0f, 100f, 10f, val => sliderVal = val, step: 5f, getter: () => sliderVal, formatValue: v => $"{v:0}%");
            Check(sliderEl.MinValue == 0f && sliderEl.MaxValue == 100f && sliderEl.Step == 5f, "Slider range and step correct");
            int dropIdx = 0;
            var options = new[]
            {
                "Option A",
                "Option B",
                "Option C"
            };
            var dropEl = section.AddDropdown("Test Dropdown", options, 0, idx => dropIdx = idx, () => dropIdx);
            Check(dropEl.Options.Count == 3 && dropEl.SelectedIndex == 0, "Dropdown options and initial index correct");
            Check(section.Elements.Count == 5, "Section contains all 5 template elements");
            // 3. Commit and Cancel lifecycle
            toggleEl.Value = true;
            Check(!toggleVal && toggleChangeCount == 0, "Editing a toggle stages the value without firing its callback");
            section.Commit();
            Check(toggleVal && toggleChangeCount == 1, "Commit propagates staged toggle value once");
            section.Commit();
            Check(toggleChangeCount == 1, "Repeated Apply without changes does not repeat callbacks");
            sliderEl.Value = 50f;
            section.Commit();
            Check(sliderVal == 50f, "Commit propagates slider value to callback");
            // Cancel
            sliderEl.Value = 99f;
            toggleEl.Value = false;
            dropEl.SelectedIndex = 2;
            section.Cancel();
            Check(sliderEl.Value == 50f && sliderVal == 50f, "Cancel discards pending slider value without applying it");
            Check(toggleEl.Value && toggleVal && toggleChangeCount == 1, "Cancel discards toggle edits without a callback");
            Check(dropEl.SelectedIndex == 0 && dropIdx == 0, "Cancel discards dropdown edits without a callback");
            sliderEl.Value = 90f;
            ((ModConfigSection)section).BeginEdit();
            Check(sliderEl.Value == 50f, "Reopening Options refreshes values from the committed source");
            // 4. BepInEx ConfigEntry integration
            string tempConfigPath = Path.Combine(Paths.ConfigPath ?? ".", "test_foundation_config.cfg");
            try
            {
                var configFile = new ConfigFile(tempConfigPath, true);
                var boolEntry = configFile.Bind("General", "TestBool", true);
                var intEntry = configFile.Bind("General", "TestInt", 42);
                var floatEntry = configFile.Bind("General", "TestFloat", 1.5f);
                var strEntry = configFile.Bind("General", "TestStr", "Option B");
                var boundToggle = section.AddToggle(boolEntry);
                var boundSlider = section.AddSlider(intEntry, 0, 100);
                var boundFloatSlider = section.AddSlider(floatEntry, 0f, 5f, step: 0.1f);
                var boundDrop = section.AddDropdown(strEntry, Array.AsReadOnly(options));
                Check(boundToggle.Value == true, "Bound toggle reads initial ConfigEntry value");
                Check(boundSlider.Value == 42f, "Bound slider reads initial ConfigEntry value");
                Check(boundFloatSlider.Value == 1.5f, "Bound float slider reads initial ConfigEntry value");
                Check(boundDrop.SelectedIndex == 1, "Bound dropdown selects matching string option index");
                boundToggle.Value = false;
                boundSlider.Value = 65f;
                boundFloatSlider.Value = 2.5f;
                boundDrop.SelectedIndex = 2;
                Check(boolEntry.Value && intEntry.Value == 42 && floatEntry.Value == 1.5f && strEntry.Value == "Option B", "Editing bound controls never changes ConfigEntry values before Apply");
                section.Cancel();
                Check(boolEntry.Value && intEntry.Value == 42 && floatEntry.Value == 1.5f && strEntry.Value == "Option B" && boundToggle.Value && boundSlider.Value == 42f && boundDrop.SelectedIndex == 1, "Cancel restores all BepInEx bound controls and values");
                boundToggle.Value = false;
                boundSlider.Value = 65f;
                boundFloatSlider.Value = 2.5f;
                boundDrop.SelectedIndex = 2;
                section.Commit();
                Check(!boolEntry.Value && intEntry.Value == 65 && floatEntry.Value == 2.5f && strEntry.Value == "Option C", "Apply writes all changed BepInEx ConfigEntry types");
            }
            finally
            {
                if (File.Exists(tempConfigPath))
                    File.Delete(tempConfigPath);
            }

            // 5. New staged text, numeric and search controls for Foundation UI.
            string persistedText = "Original";
            double persistedNumber = 10;
            int persistedSearch = 0;
            var input = section.AddInput("Text Entry", persistedText, val => persistedText = val, () => persistedText, maxLength: 64);
            var number = section.AddNumber("Numeric Entry", 0, 100, persistedNumber, 0.5, val => persistedNumber = val, () => persistedNumber);
            var search = section.AddSearch("Search Entry", new[] { "Alpha", "Beta", "Gamma" }, 0, idx => persistedSearch = idx, () => persistedSearch);
            input.Value = "Changed";
            number.Value = 25;
            search.SelectedIndex = 2;
            Check(persistedText == "Original" && persistedNumber == 10 && persistedSearch == 0, "Text, number and searchable selection stage without mutating committed values");
            section.Cancel();
            Check(input.Value == "Original" && number.Value == 10 && search.SelectedIndex == 0, "Cancel restores staged text/numeric/search values");
            input.Value = "Changed";
            number.Value = 25;
            search.SelectedIndex = 2;
            section.Commit();
            Check(persistedText == "Changed" && persistedNumber == 25 && persistedSearch == 2, "Apply writes staged text, numeric and searchable values");
            section.Commit();
            Check(persistedText == "Changed" && persistedNumber == 25 && persistedSearch == 2, "Repeated apply is idempotent for new controls");
            // 6. Remove section
            registry.RemoveSection("test.mod");
            Check(registry.Sections.Count == 0 && !registry.TryGetSection("test.mod", out _), "RemoveSection successfully cleans up");
        }

        void VerifyFoundationPanels()
        {
            // Pure registry tests run before a world is created; never open a popup
            // or modify user machines in the isolated native smoke test.
            var registry = new FoundationPanelRegistry();
            Check(registry.Registrations.Count == 0, "UI panel registry starts empty");
            int notifications = 0;
            registry.Changed += () => notifications++;
            Action<IFoundationPanelBuilder> build = panel =>
            {
            };
            var generic = registry.Register(new FoundationPanelDefinition("ui.test", "general", "General", build));
            var machine = registry.Register(new FoundationPanelDefinition("ui.test", "all-machines", "All Machines", build, typeof(FlowElement)));
            var specialized = registry.Register(new FoundationPanelDefinition("ui.native", "switch", "Switch", build, typeof(TrackSwitch), -100));
            Check(registry.Registrations.Count == 3, "UI panel registry tracks independent mod windows and machine definitions");
            Check(registry.TryGet("ui.test", "general", out var general) && ReferenceEquals(general, generic.Definition), "UI panel registry resolves mod-owned window by stable identity");
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var resolved) && ReferenceEquals(resolved, specialized.Definition), "UI panel registry prefers native subtype over inherited fallback");
            Check(registry.TryResolveMachine(typeof(SingleButton), out var fallback) && ReferenceEquals(fallback, machine.Definition), "UI machine registry falls back to FlowElement");
            var overrideSwitch = registry.Register(new FoundationPanelDefinition("ui.mod", "switch", "Override", build, typeof(TrackSwitch), 200));
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var overrideFound) && ReferenceEquals(overrideFound, overrideSwitch.Definition), "UI machine registry respects priority for equal machine type");
            bool rejected = false;
            try
            {
                registry.Register(new FoundationPanelDefinition("ui.mod", "switch", "Duplicate", build, typeof(TrackSwitch)));
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, "UI panel rejects duplicate owner/local ID");
            rejected = false;
            try
            {
                new FoundationPanelDefinition("ui.mod", "bad", "Bad", build, typeof(string));
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, "UI panel rejects non-machine type binding");
            overrideSwitch.Dispose();
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var restored) && ReferenceEquals(restored, specialized.Definition), "UI disposal restores registered native default");
            using (var owner = registry.ForMod("ui.consumer"))
            {
                var a = owner.Register("one", "First", build);
                var b = owner.Register("two", "Second", build, typeof(SingleButton));
                Check(registry.Registrations.Count == 5 && a.IsRegistered && b.IsRegistered, "UI mod context owns multiple window and machine registrations");
            }

            Check(registry.Registrations.Count == 3 && registry.RemoveOwner("ui.consumer") == 0, "UI owner disposal unregisters every owned window");
            Check(registry.RemoveOwner("ui.test") == 2, "UI owner removal does not remove other mods");
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var remaining) && ReferenceEquals(remaining, specialized.Definition), "UI foreign mod's specialized panel survives owner removal");
            specialized.Dispose();
            generic.Dispose();
            machine.Dispose();
            Check(registry.Registrations.Count == 0 && notifications >= 10, "UI panel registration cleanup is complete and produces notifications");
            var readOnly = new FoundationUiProperty("health", "Health", typeof(float), () => 1f);
            Check(readOnly.Write == null && readOnly.ValueType == typeof(float), "UI dynamic property can be read-only without reflection");
            rejected = false;
            try
            {
                new FoundationUiProperty("bad", "Bad", typeof(DateTime), () => DateTime.Now);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, "UI dynamic editor rejects unsupported property types");
        }

        IEnumerator VerifyRequirementNavigation(TechTreeMenu menu, TechTreeTabs tabs, Camera camera, string directory)
        {
            long gold = GamePlayer.Current.GetCurrency(Currency.Gold);
            menu.FocusNode(TechNode.Get("supply_depot"));
            yield return null;
            var native = Node(menu, "supply_depot");
            native.UpdateNodeStatus();
            var label = TechTreeTabs.Field<TMP_Text>(native, "_requirementsText");
            label.ForceMeshUpdate();
            Check(label.textInfo.linkCount == 1 && label.GetParsedText().Contains("Requires"), "Original requirement label retains native translated text with a clickable target");
            camera.transform.position = new Vector3(native.Tech.Position.x, native.Tech.Position.y, -10);
            yield return null;
            var position = camera.transform.position;
            RequirementClick(native, camera, 0, PointerEventData.InputButton.Right);
            Check(menu.RootNode == null && camera.transform.position == position, "Right clicking a requirement does not navigate");
            RequirementClick(native, camera, 0);
            var automation = TechNode.Get("mine_automation");
            Check(!tabs.ModsSelected && menu.RootNode == automation.Root && menu.Nodes.Any(n => n.Tech == automation), "Native prerequisite click opens its correct Base Game branch");
            CheckFocused(camera, automation, "Native prerequisite click centers the destination");
            using (var origin = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.origin", "Origin Technology")))
            using (var source = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.requires", "Requirement Navigation", new Vector2(1300, 0), research: new TechNodeOptions(requirements: new[] { "requisition_office", "urakka.foundation.example.upgrade", origin.Id }))))
            {
                var sourceModel = ((INativeTechNode)source).NativeNode;
                menu.FocusNode(sourceModel);
                yield return null;
                var view = Node(menu, source.Id);
                view.UpdateNodeStatus();
                label = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText");
                label.ForceMeshUpdate();
                Check(label.textInfo.linkCount == 3, "Each missing prerequisite gets its own destination");
                Capture(camera, System.IO.Path.Combine(directory, "foundation-requirement-links.png"));
                RequirementClick(view, camera, 0);
                var requisition = TechNode.Get("requisition_office");
                Check(!tabs.ModsSelected && menu.RootNode == requisition.Root && menu.Nodes.Any(n => n.Tech == requisition), "Requires Requisitionist click switches to Base Game and shows Requisitionist");
                CheckFocused(camera, requisition, "Requires Requisitionist centers its actual research node");
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id);
                view.UpdateNodeStatus();
                RequirementClick(view, camera, 1);
                var upgrade = TechNode.Get("urakka.foundation.example.upgrade");
                Check(tabs.ModsSelected && menu.RootNode == upgrade.Root && menu.Nodes.Any(n => n.Tech == upgrade), "Second requirement opens the correct Mods upgrade branch");
                CheckFocused(camera, upgrade, "Second requirement centers the correct node rather than the first requirement");
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id);
                view.UpdateNodeStatus();
                RequirementClick(view, camera, 2);
                Check(tabs.ModsSelected && menu.RootNode == null, "Requirement navigation supports a framework root at the default origin");
                CheckFocused(camera, ((INativeTechNode)origin).NativeNode, "Default-origin requirement centers without a null parent");
                Check(GamePlayer.Current.GetCurrency(Currency.Gold) == gold && !sourceModel.IsPurchased && !upgrade.IsPurchased && !requisition.IsPurchased, "Requirement navigation purchases no research and spends no gold");
                GamePlayer.Current.AddTech(origin.Id);
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id);
                view.UpdateNodeStatus();
                label = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText");
                label.ForceMeshUpdate();
                Check(label.textInfo.linkCount == 2 && !label.GetParsedText().Contains("Origin Technology"), "Purchased prerequisites disappear and remaining link destinations refresh");
                RequirementClick(view, camera, 1);
                Check(menu.RootNode == upgrade.Root, "Remaining second requirement keeps the correct destination after purchase");
            }
        }

        void CheckFocused(Camera camera, TechNode node, string name)
        {
            var point = node.Position == Vector2.zero && node.Root != null ? node.Root.Position : node.Position;
            Check(Vector3.Distance(camera.transform.position, new Vector3(point.x, point.y, -10)) < .01f, name);
        }

        void RequirementClick(UITechNode view, Camera camera, int linkIndex, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            Canvas.ForceUpdateCanvases();
            var text = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText");
            text.ForceMeshUpdate();
            var link = text.textInfo.linkInfo[linkIndex];
            var character = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength / 2];
            var world = text.transform.TransformPoint((character.bottomLeft + character.topRight) * .5f);
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, world),
                button = button
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            var hit = hits.FirstOrDefault();
            Check(hit.gameObject && hit.gameObject.GetComponentInParent<RequirementLinks>(), "Requirement label receives the first pointer raycast: " + view.Tech.Identifier + " / " + linkIndex);
            data.pointerPressRaycast = hit;
            data.pointerCurrentRaycast = hit;
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        readonly List<string> checks = new List<string>();
        readonly List<string> errors = new List<string>();
        void Check(bool pass, string name)
        {
            if (!pass)
                throw new InvalidOperationException(name);
            checks.Add(name);
        }

        void WatchErrors(string message, string stack, LogType type)
        {
            if (type == LogType.Exception)
                errors.Add(message + "\n" + stack);
        }

        UITechNode Node(TechTreeMenu menu, string id) => menu.Nodes.Single(n => n.Tech.Identifier == id);
        IEnumerator RuntimeCheck()
        {
            Application.logMessageReceived += WatchErrors;
            yield return new WaitForSecondsRealtime(5);
            string directory = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification");
            Directory.CreateDirectory(directory);
            string error = null;
            Mouse mouse = null;
            TechTreeMenu menu = null;
            TechTreeTabs tabs = null;
            Camera camera = null;
            Vector3 basePosition = default;
            float baseSize = 0;
            int initialResearchCount = 0;
            TechNode first = null, upgrade = null;
            const string FirstId = "urakka.foundation.example.first", UpgradeId = "urakka.foundation.example.upgrade";
            try
            {
                Check(GamePlayer.Current == null && !GameUI.Instance, "No user kingdom loaded");
                Check(!SaveGame.LoadLatestSave(), "Save loading blocked in diagnostic runtime");
                VerifyRegistry();
                initialResearchCount = FoundationApi.TechNodes.Nodes.Count;
                VerifyWorkspaceRegistry();
                VerifyModConfig();
                VerifyFoundationPanels();
                VerifyFoundationCore();
                var player = GamePlayer.CreateNewPlayer();
                player.Flows[0].Label = "Foundation native research verification";
                SceneManager.LoadScene("Game");
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }

            yield return new WaitForSecondsRealtime(4);
            if (error == null)
            {
                var updateChecks = GameUpdateChecks.Run(Check);
                while (error == null)
                {
                    bool more = false;
                    object step = null;
                    try
                    {
                        more = updateChecks.MoveNext();
                        if (more)
                            step = updateChecks.Current;
                    }
                    catch (Exception ex)
                    {
                        error = ex.ToString();
                    }

                    if (!more || error != null)
                        break;
                    yield return step;
                }

                (updateChecks as IDisposable)?.Dispose();
            }

            if (error == null)
                try
                {
                    mouse = InputSystem.AddDevice<Mouse>();
                    mouse.MakeCurrent();
                    VerifyModBar(directory);
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            // Native dropdowns remove their blocker after the closing fade.
            yield return new WaitForSecondsRealtime(.25f);
            if (error == null)
                try
                {
                    FinishModBarChecks(directory);
                    VerifyWorkspaceLocked();
                    GameUI.Instance.ToggleTechMenu(true);
                    menu = TechTreeMenu.Instance;
                    tabs = Tabs(menu);
                    camera = TechTreeTabs.Field<Camera>(menu, "_camera");
                    Check(tabs && tabs.Bar.GetComponentsInChildren<Button>().Length == 2, "Tech Tree heading has two tabs");
                    Check(tabs.Heading.GetComponentInChildren<TMPro.TMP_Text>().text == "Tech Tree" && tabs.ResearchButton.GetComponentInChildren<TMPro.TMP_Text>().text == "Base Game" && tabs.ModsButton.GetComponentInChildren<TMPro.TMP_Text>().text == "Mods", "Requested heading and tab labels retained");
                    var headingCorners = new Vector3[4];
                    tabs.Heading.GetWorldCorners(headingCorners);
                    var hudCorners = new Vector3[4];
                    testedBar.Bar.GetWorldCorners(hudCorners);
                    Check(headingCorners[1].y < hudCorners[0].y, "Tech Tree heading stays below the shared Mod Bar");
                    Check(menu.Nodes.Any() && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Base Game contains only original and existing mod research");
                    basePosition = camera.transform.position;
                    baseSize = camera.orthographicSize;
                    Capture(camera, Path.Combine(directory, "foundation-tech-tree.png"));
                    GamePlayer.Current.AddTech("global_tech_upgrade");
                    menu.UpdateTechListButton();
                    Click(tabs.ModsButton);
                    tabs.SelectMod("urakka.foundation.example", false);
                    first = TechNode.Get(FirstId);
                    upgrade = TechNode.Get(UpgradeId);
                    Check(tabs.ModsSelected && menu.Nodes.Count() == 2 && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == "urakka.foundation.example"), "Selected mod category renders only its own two root technologies");
                    Check(!tabs.ModsPage.GetComponent<ScrollRect>() && menu.Nodes.All(n => n.transform.parent == TechTreeTabs.Field<RectTransform>(menu, "_nodeParent")), "Mod research uses the native world canvas, without overlay cards or scroll containers");
                    var prefab = TechTreeTabs.Field<UITechNode>(menu, "_elementNodePrefab");
                    var rootView = Node(menu, FirstId);
                    Check(rootView.GetType() == prefab.GetType() && ((RectTransform)rootView.transform).sizeDelta == ((RectTransform)prefab.transform).sizeDelta, "Root node uses the exact native prefab type and dimensions");
                    Check(rootView.Tech == first && ((RectTransform)rootView.transform).anchoredPosition == first.Position, "Native SetTech positions node in game coordinates");
                    Check(rootView.GetTooltipTitle() == first.Name, "Native tooltip reads the mod research title");
                    rootView.UpdateNodeStatus();
                    Check(TechTreeTabs.Field<Image>(rootView, "_glow").gameObject.activeSelf && !TechTreeTabs.Field<Image>(rootView, "_purchased").gameObject.activeSelf, "Native available glow and unpurchased state");
                    rootView.OnPointerEnter(new PointerEventData(EventSystem.current));
                    Check(TechTreeTabs.Field<Image>(rootView, "_highlight").gameObject.activeSelf, "Native hover highlight works");
                    rootView.OnPointerExit(new PointerEventData(EventSystem.current));
                    Check(!TechTreeTabs.Field<Image>(rootView, "_highlight").gameObject.activeSelf, "Native hover highlight clears");
                    Check(upgrade.IsLocked, "Child research locked until its native prerequisite is purchased");
                    menu.ShowTechTree(first, true);
                    var locked = Node(menu, UpgradeId);
                    locked.UpdateNodeStatus();
                    Check(TechTreeTabs.Field<Image>(locked, "_icon").sprite == TechTreeTabs.Field<Sprite>(locked, "_lockedIcon"), "Unpurchased prerequisite uses native locked icon");
                    Check(TechTreeTabs.Field<RectTransform>(menu, "_nodeParent").GetComponentsInChildren<LineRenderer>().Any(), "Native branch rendering draws the prerequisite connector");
                    menu.ShowTechTree(null, true);
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            if (error == null)
            {
                var navigation = VerifyModCategories(menu, tabs, camera, directory);
                while (error == null)
                {
                    bool more = false;
                    object step = null;
                    try
                    {
                        more = navigation.MoveNext();
                        if (more)
                            step = navigation.Current;
                    }
                    catch (Exception ex)
                    {
                        error = ex.ToString();
                    }

                    if (!more || error != null)
                        break;
                    yield return step;
                }

                (navigation as IDisposable)?.Dispose();
                navigation = VerifyRequirementNavigation(menu, tabs, camera, directory);
                while (error == null)
                {
                    bool more = false;
                    object step = null;
                    try
                    {
                        more = navigation.MoveNext();
                        if (more)
                            step = navigation.Current;
                    }
                    catch (Exception ex)
                    {
                        error = ex.ToString();
                    }

                    if (!more || error != null)
                        break;
                    yield return step;
                }

                (navigation as IDisposable)?.Dispose();
            }

            if (error == null)
                try
                {
                    menu.FocusNode(TechNode.Get("mine"));
                    menu.ShowTechTree(null, true);
                    camera.transform.position = basePosition;
                    tabs.Select(true, false);
                    menu.ShowTechTree(null, true);
                    camera.transform.position = new Vector3(0, 0, -10);
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            if (error == null)
                try
                {
                    var player = GamePlayer.Current;
                    Capture(camera, Path.Combine(directory, "foundation-mods-available.png"));
                    long gold = player.GetCurrency(Currency.Gold);
                    player.AddCurrency(Currency.Gold, 50 - gold, false);
                    NativeClick(Node(menu, FirstId), camera);
                    Check(!first.IsPurchased && player.GetCurrency(Currency.Gold) == 50, "Native insufficient-funds path leaves research and currency unchanged");
                    player.AddCurrency(Currency.Gold, 1000, false);
                    NativeClick(Node(menu, FirstId), camera);
                    Check(first.IsPurchased && player.GetCurrency(Currency.Gold) == 950, "Native click buys research and spends the specified gold cost (purchased=" + first.IsPurchased + ", gold=" + player.GetCurrency(Currency.Gold) + ")");
                    Check(first.Name == "Example Researched", "Native unlock event reaches the separately compiled consumer plugin");
                    Check(!upgrade.IsLocked && upgrade.IsAvailable, "Native prerequisite unlock makes the child available");
                    menu.UpdateTechTree();
                    Check(TechTreeTabs.Field<Image>(Node(menu, FirstId), "_purchased").gameObject.activeSelf, "Native purchased checkmark appears");
                    NativeClick(Node(menu, FirstId), camera);
                    Check(menu.RootNode == first && menu.Nodes.Any(n => n.Tech == upgrade), "Clicking researched root opens its native upgrade branch");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            if (error == null)
                try
                {
                    NativeClick(Node(menu, UpgradeId), camera);
                    Check(upgrade.IsPurchased && GamePlayer.Current.GetCurrency(Currency.Gold) == 925, "Native upgrade purchase spends cost and saves research state");
                    Check(menu.ProcessEscape() && menu.RootNode == null && tabs.ModsSelected, "Native Escape returns from branch to Mods root map");
                    var native = ((INativeTechNode)FoundationApi.TechNodes.Nodes.Single(n => n.Id == FirstId)).NativeNode;
                    Check(native == first, "Public interface exposes the actual native research model");
                    var player = GamePlayer.Current;
                    var json = player.ToJson();
                    Check(json["Tech"].AsJsonArray.Any(v => v.AsString == FirstId) && json["Tech"].AsJsonArray.Any(v => v.AsString == UpgradeId), "Purchased mod technologies use the game's existing saved ID list");
                    Check(!json["NewTechCount"].AsJsonObject.ContainsKey(FirstId), "Optional mod branch notification counters do not create missing-key save failures");
                    try
                    {
                        var restored = GamePlayer.FromJson(json);
                        Check(restored.HasTech(first) && restored.HasTech(upgrade), "Native JSON round trip retains mod research purchases");
                    }
                    finally
                    {
                        GamePlayer.Current = player;
                    }

                    var pos = camera.transform.position;
                    var size = camera.orthographicSize;
                    WorldFlowCamera.Instance.Move(100, 80);
                    Check(camera.transform.position != pos, "Native map panning works on Mods");
                    Wheel(mouse, -120);
                    Check(camera.orthographicSize > size, "Real mouse wheel uses native Mods map zoom");
                    Wheel(mouse, 120);
                    var modPos = camera.transform.position;
                    var modSize = camera.orthographicSize;
                    Click(tabs.ResearchButton);
                    Check(!tabs.ModsSelected && Vector3.Distance(camera.transform.position, basePosition) < .01f && Mathf.Approximately(camera.orthographicSize, baseSize), "Switching to Base Game restores its native camera position and zoom");
                    Check(menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Native mod research stays out of Base Game after purchases");
                    Click(tabs.ModsButton);
                    Check(Vector3.Distance(camera.transform.position, modPos) < .01f && Mathf.Approximately(camera.orthographicSize, modSize), "Returning to Mods restores its independent map position and zoom");
                    camera.transform.position = new Vector3(0, 0, -10);
                    var icon = TechNode.Get("mine").Icon;
                    diagnosticNode = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.visible", "Late Registered", "urakka.foundation.example", "Foundation Example", new Vector2(-300, -220), "A real game tech node registered after the kingdom was loaded.", icon, new TechNodeOptions(fixedCost: new CurrencyCollection())));
                    Check(!((INativeTechNode)diagnosticNode).NativeNode.IsPurchased, "Late registration safely extends the player's native research storage");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            if (error == null)
                try
                {
                    var late = Node(menu, diagnosticNode.Id);
                    Check(late.Tech.Name == "Late Registered" && late.Tech.Icon == diagnosticNode.Definition.Icon, "Live registration automatically creates native view with caller-owned icon");
                    var definition = diagnosticNode.Definition;
                    diagnosticNode.Update(new TechNodeDefinition(diagnosticNode.Id, "Native Technology", definition.Position, definition.Description, definition.Icon, ((INativeTechNodeDefinition)definition).Research));
                    menu.ToggleListView();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            yield return null;
            if (error == null)
                try
                {
                    Check(Node(menu, diagnosticNode.Id).Tech.Name == "Native Technology", "Definition updates refresh the native research view automatically");
                    var list = TechTreeTabs.Field<TechTreeListView>(menu, "_listView");
                    Check(list.gameObject.activeSelf, "Native research list available on Mods page");
                    Check(list.GetComponentsInChildren<TechTreeListItem>().All(item => NativeResearch.Active(TechTreeTabs.Field<TechNode>(item, "_node"))), "Native Mods list filters out Base Game research");
                    menu.ToggleListView();
                    Capture(camera, Path.Combine(directory, "foundation-mods.png"));
                    diagnosticNode.Dispose();
                    tabs.ModTree.Refresh();
                    Check(!menu.Nodes.Any(n => n.Tech.Identifier == diagnosticNode.Id), "Disposal removes native view without reindexing saved research");
                    Check(GamePlayer.Current.HasTech(first) && GamePlayer.Current.HasTech(upgrade), "Late registration and disposal preserve existing purchased research");
                    menu.FocusNode(first);
                    Check(tabs.ModsSelected && menu.RootNode == null, "Native focus routes a mod root to the correct tab");
                    menu.FocusNode(TechNode.Get("mine"));
                    Check(!tabs.ModsSelected, "Native focus routes original research back to Base Game");
                    for (int i = 0; i < 2; i++)
                    {
                        Click(tabs.ModsButton);
                        GameUI.Instance.ToggleTechMenu(false);
                        GameUI.Instance.ToggleTechMenu(true);
                        Check(!tabs.ModsSelected && menu.GetComponents<TechTreeTabs>().Length == 1 && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Reopening starts on Base Game without duplicate UI " + i);
                    }

                    Check(menu.ProcessEscape() && !TechTreeMenu.Visible, "Native Escape closes the main research map");
                    PrepareWorkspacePlacement();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return null;
            if (error == null)
                try
                {
                    ExerciseWorkspace();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(.3f);
            if (error == null)
                try
                {
                    FinishWorkspace(directory);
                    SceneManager.LoadScene("MainMenu");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            if (mouse != null)
                InputSystem.RemoveDevice(mouse);
            yield return new WaitForSecondsRealtime(2);
            if (error == null)
                try
                {
                    Check(!menu && !tabs, "Leaving the game scene destroys Foundation UI cleanly");
                    Check(!testedBar && lifecycleElement.Disposes == 1 && lifecycleItem.IsRegistered, "Scene teardown disposes the custom Mod Bar view once and preserves its registration");
                    Check(FoundationApi.TechNodes.Nodes.Count == initialResearchCount, "Native registrations survive scene teardown");
                    var previewPlayer = GamePlayer.CreateNewPlayer();
                    previewPlayer.AddCurrency(Currency.Gold, 123456, false);
                    SceneManager.LoadScene("Game");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(3);
            TMPro.TMP_Dropdown reopenedDropdown = null;
            if (error == null)
                try
                {
                    var hosts = GameUI.Instance.GetComponentsInChildren<ModBarHost>(true);
                    Check(hosts.Length == 1 && lifecycleElement.Creates == 2 && lifecycleElement.Disposes == 1, "New game scene recreates one Mod Bar and a fresh custom view from the surviving registration");
                    reopenedDropdown = hosts[0].Slots.Single(s => s.name == "urakka.foundation.example:period").GetComponentInChildren<TMPro.TMP_Dropdown>();
                    Check(reopenedDropdown.value == 1, "Mod-owned dropdown state survives HUD recreation");
                    reopenedDropdown.Show();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(.25f);
            if (error == null)
                try
                {
                    Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-dropdown.png"));
                    GameUI.Instance.ProcessEscape();
                    Check(!GameUI.MenuVisible, "Escape dismisses the Mod Bar dropdown without opening the game menu");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(.25f);
            if (error == null)
                try
                {
                    Check(!reopenedDropdown.IsExpanded, "Mod Bar dropdown completes its native close and removes its blocker");
                    reopenedDropdown.Show();
                    GameUI.Instance.ToggleUI(false);
                    GameUI.Instance.ToggleUI(true);
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(.25f);
            if (error == null)
                try
                {
                    Check(!reopenedDropdown.IsExpanded, "Hiding the native HUD also closes Mod Bar popups");
                    SceneManager.LoadScene("MainMenu");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            yield return new WaitForSecondsRealtime(2);
            if (error == null)
                try
                {
                    Check(lifecycleElement.Disposes == 2 && lifecycleItem.IsRegistered, "Each HUD lifetime disposes its custom view exactly once");
                    lifecycleItem.Dispose();
                    Check(errors.Count == 0, "No Unity exceptions during research, workspace and Mod Bar diagnostics");
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

            var report = new JsonObject
            {
                {
                    "passed",
                    error == null
                },
                {
                    "version",
                    Version
                },
                {
                    "gameVersion",
                    Application.version
                },
                {
                    "unityVersion",
                    Application.unityVersion
                },
                {
                    "bepInExVersion",
                    typeof(BaseUnityPlugin).Assembly.GetName().Version.ToString()
                },
                {
                    "dllSha256",
                    HashFile(typeof(FoundationPlugin).Assembly.Location)
                },
                {
                    "count",
                    checks.Count
                },
                {
                    "checks",
                    new JsonArray(checks.Select(c => (JsonValue)c).ToArray())
                },
                {
                    "plugins",
                    new JsonArray(BepInEx.Bootstrap.Chainloader.PluginInfos.Values.Select(p => (JsonValue)(p.Metadata.Name + " " + p.Metadata.Version)).ToArray())
                },
                {
                    "error",
                    error ?? ""
                },
                {
                    "exceptions",
                    new JsonArray(errors.Select(c => (JsonValue)c).ToArray())
                }
            };
            File.WriteAllText(Path.Combine(directory, "runtime-tests.json"), report.ToString());
            Application.logMessageReceived -= WatchErrors;
            if (error != null)
                Logger.LogError(error);
            else
                Logger.LogInfo("Foundation native diagnostic passed: " + checks.Count + " checks.");
            Application.Quit();
        }

        void NativeClick(UITechNode node, Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            var center = node.transform.TransformPoint(((RectTransform)node.transform).rect.center);
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, center),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Check(hits.Any(hit => hit.gameObject.GetComponentInParent<UITechNode>()?.Tech == node.Tech), "Native research node receives pointer raycasts: " + node.Tech.Identifier);
            ExecuteEvents.Execute(node.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var data = new PointerEventData(EventSystem.current)
            {
                position = point,
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Tab receives pointer raycasts: " + button.name);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        static void Wheel(Mouse mouse, float delta)
        {
            InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.zero);
            InputSystem.Update();
            InputSystem.QueueDeltaStateEvent(mouse.scroll, new Vector2(0, delta));
            InputSystem.Update();
            InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.zero);
            InputSystem.Update();
        }

        static string HashFile(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static void Capture(Camera camera, string path)
        {
            Canvas.ForceUpdateCanvases();
            var overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var cameras = overlays.Select(c => c.worldCamera).ToArray();
            var distances = overlays.Select(c => c.planeDistance).ToArray();
            var orders = overlays.Select(c => c.sortingOrder).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            var oldTarget = camera.targetTexture;
            int oldMask = camera.cullingMask;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in overlays)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10;
                    canvas.sortingOrder = 30000;
                }

                camera.cullingMask = -1;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.cullingMask = oldMask;
                for (int i = 0; i < overlays.Length; i++)
                {
                    overlays[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    overlays[i].worldCamera = cameras[i];
                    overlays[i].planeDistance = distances[i];
                    overlays[i].sortingOrder = orders[i];
                }

                Canvas.ForceUpdateCanvases();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(texture);
            }
        }

        void InstallDiagnostics()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var safety = new Harmony(Id + ".diagnostics");
            foreach (var type in new[]
            {
                typeof(global::KingdomFoundation.NoSave),
                typeof(global::KingdomFoundation.NoAutosave),
                typeof(global::KingdomFoundation.NoLoad),
                typeof(global::KingdomFoundation.NoAchievement),
                typeof(global::KingdomFoundation.Offline)
            }

            )
                safety.CreateClassProcessor(type).Patch();
        }

        ITechNode diagnosticNode;
        static int FoundationApiNodeCount() => FoundationApi.TechNodes.Nodes.Count;
        void Throws<T>(Action action, string name)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                Check(true, name);
                return;
            }

            throw new InvalidOperationException(name + ": expected " + typeof(T).Name);
        }

        void VerifyRegistry()
        {
            ITechNodeRegistry registry = new TechNodeRegistry();
            int changes = 0;
            registry.Changed += () => changes++;
            Throws<ArgumentNullException>(() => registry.CreateTechNode(null), "Null definitions rejected");
            Throws<ArgumentException>(() => new TechNodeDefinition(" ", "Title"), "Blank identifiers rejected");
            Throws<ArgumentException>(() => new TechNodeDefinition("example.one", " "), "Blank titles rejected");
            Throws<ArgumentOutOfRangeException>(() => new TechNodeDefinition("example.one", "Title", new Vector2(float.NaN, 0)), "Nonfinite positions rejected");
            global::KingdomFoundation.MutableDefinition definition = new global::KingdomFoundation.MutableDefinition
            {
                Id = "example.first",
                Title = "First"
            };
            var first = registry.CreateTechNode(definition);
            definition.Title = "Changed behind registry";
            Check(first.Definition.Title == "First" && first.Definition.Description == "", "Custom interface definitions are validated and snapshotted");
            var snapshot = registry.Nodes;
            var second = registry.CreateTechNode(new TechNodeDefinition("other.second", "Second"));
            Check(snapshot.Count == 1 && registry.Nodes.Count == 2, "Registry enumerations are stable snapshots");
            Check(registry.TryGet(first.Id, out var found) && ReferenceEquals(found, first), "Identifier lookup returns original interface handle");
            Throws<ArgumentException>(() => registry.CreateTechNode(new TechNodeDefinition(first.Id, "Collision")), "Duplicate identifiers preserve the owning mod's registration");
            Check(registry.Nodes.Count == 2 && ReferenceEquals(registry.Nodes[0], first), "Rejected duplicate does not replace existing node");
            first.Update(new TechNodeDefinition(first.Id, "Updated", new Vector2(200, 100)));
            Check(first.Definition.Title == "Updated" && first.Definition.Position == new Vector2(200, 100) && changes == 3, "Update changes definition and notifies subscribers once");
            Throws<ArgumentException>(() => first.Update(new TechNodeDefinition("changed.identity", "Wrong")), "Updates cannot change node identity");
            Check(first.Definition.Title == "Updated", "Rejected update preserves prior definition");
            first.Dispose();
            first.Dispose();
            Check(!first.IsRegistered && registry.Nodes.Count == 1 && changes == 4, "Disposal unregisters exactly once");
            Throws<ObjectDisposedException>(() => first.Update(new TechNodeDefinition(first.Id, "Gone")), "Disposed handles cannot update registry");
            var replacement = registry.CreateTechNode(new TechNodeDefinition(first.Id, "Replacement"));
            first.Dispose();
            Check(replacement.IsRegistered && registry.Nodes.Count == 2, "Stale disposal cannot remove a replacement with reused identifier");
            Check(!registry.TryGet(null, out _) && !registry.TryGet("missing", out _), "Missing identifiers are safe to query");
            replacement.Dispose();
            second.Dispose();
            Check(registry.Nodes.Count == 0, "Each mod can clean up its own registrations");
            Check(Chainloader.PluginInfos.ContainsKey("urakka.foundation.example"), "Separate example plugin loaded with a Foundation dependency");
            Check(FoundationApi.TechNodes.Nodes.Count(n => n.Id.StartsWith("urakka.foundation.example.")) == 3, "Separate example plugin registers native research before any game scene");
            Check(FoundationApi.TechNodes.Nodes.All(n => ReferenceEquals(((INativeTechNode)n).NativeNode, TechNode.Get(n.Id))), "Public handles refer to the actual game research catalog");
            Throws<ArgumentException>(() => FoundationApi.CreateTechNode(new TechNodeDefinition("mine", "Collision")), "API cannot replace a native game technology");
            Throws<ArgumentOutOfRangeException>(() => new TechNodeOptions(fixedCost: new CurrencyCollection(Currency.Gold, -1)), "Negative research costs rejected");
        }

        const string RelayId = "urakka.foundation.example.relay";
        IWorkspaceNodeType relayType;
        WorkspaceNode relayA, relayB;
        WorldFlowGhost relayGhost;
        FlowPipe relayPipe;
        Keyboard workspaceKeyboard;
        long workspaceGold;
        int workspaceCount;
        void VerifyWorkspaceRegistry()
        {
            var registry = FoundationApi.WorkspaceNodes;
            int registeredCount = registry.Nodes.Count;
            Check(registry.Nodes.Count(n => n.Id == RelayId) == 1 && registry.TryGet(RelayId, out relayType), "Separate consumer registers a workspace node type through public interfaces");
            Throws<ArgumentNullException>(() => registry.RegisterWorkspaceNode(null), "Null workspace definitions rejected");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(relayType.Definition), "Duplicate workspace IDs rejected without replacing owner");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("Mine", "Conflict", () => null, "mine")), "Workspace registration cannot replace a native building");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("diagnostic.unknown", "Unknown", () => null, "missing.research")), "Unknown required research rejected");
            Throws<InvalidOperationException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("diagnostic.null_factory", "Invalid", () => null, "mine")), "Null factory result rejected and registration rolled back");
            Check(!registry.TryGet("diagnostic.null_factory", out _) && registry.Nodes.Count == registeredCount, "Failed factory leaves registry unchanged");
            var a = relayType.CreateInstance();
            var b = relayType.CreateInstance();
            Check(a != b && a.Guid != b.Guid && a.ElementType == RelayId && b.ElementType == RelayId, "Workspace factory creates independent models with stable type IDs and fresh GUIDs");
            Check(a is FlowElement && a.Label == "Example Relay" && a.Inputs.Count == 1 && a.Outputs.Count == 1, "External WorkspaceNode subclass is a native model with native ports");
            // Verify the new public inspector against actual native mod-owned ports.
            var inspector = FoundationApi.Flow;
            var view = inspector.Inspect(a);
            Check(view.Guid == a.Guid && view.TypeId == RelayId && view.Inputs.Count == 1 && view.Outputs.Count == 1, "Flow inspector captures native element identity and IO directions");
            var inPort = inspector.Inspect(a.Inputs[0]);
            Check(inPort.Direction == FlowPortDirection.Input && inPort.Index == 0 && inPort.BufferCapacity == a.Inputs[0].BufferSize && !inPort.IsConnected, "Port inspector reports native input index, buffer capacity and connection state");
            int connected = 0, disconnected = 0;
            Action<FlowPipeSnapshot> onConnect = p =>
            {
                if (p.OutputElementGuid == a.Guid && p.InputElementGuid == b.Guid)
                    connected++;
            };
            Action<FlowPipeSnapshot> onDisconnect = p =>
            {
                if (p.OutputElementGuid == a.Guid && p.InputElementGuid == b.Guid)
                    disconnected++;
            };
            inspector.PipeConnected += onConnect;
            inspector.PipeDisconnected += onDisconnect;
            try
            {
                var probe = new FlowPipe();
                probe.Connect(a.Outputs[0], b.Inputs[0], ghost: true);
                Check(connected == 0 && a.Outputs[0].ConnectedPipe == null, "Ghost pipes do not publish real connection events");
                probe.Connect(a.Outputs[0], b.Inputs[0]);
                Check(connected == 1 && inspector.Inspect(a.Outputs[0]).IsConnected && inspector.Inspect(probe).IsConnected, "Real native pipe connections publish a single event and update inspector state");
                var diagram = new FlowDiagram();
                diagram.Elements.Add(a);
                diagram.Elements.Add(b);
                diagram.Pipes.Add(probe);
                Check(inspector.InspectElements(diagram).Count == 2 && inspector.InspectPipes(diagram).Count == 1, "Inspector enumerates actual diagram elements and pipes");
                diagram.DisconnectPipe(probe);
                Check(disconnected == 1 && !inspector.Inspect(a.Outputs[0]).IsConnected, "Native disconnect publishes a single event after clearing the connection");
            }
            finally
            {
                inspector.PipeConnected -= onConnect;
                inspector.PipeDisconnected -= onDisconnect;
            }
        }

        void VerifyWorkspaceLocked()
        {
            VerifyMissingWorkspaceRecovery();
            Check(FlowElement.GetAllTypes().Count(n => n.ElementType == RelayId) == 1, "Native catalog includes custom workspace type exactly once");
            Check(!FlowElement.GetBuildableTypes().Any(n => n.ElementType == RelayId), "Native build menu hides workspace type before its research is unlocked");
            Throws<InvalidOperationException>(() => relayType.BeginPlacement(), "Programmatic blueprint placement also respects research lock");
            var model = FlowElement.Create(RelayId);
            Check(model is WorkspaceNode && model.RequiredTech == TechNode.Get("urakka.foundation.example.first"), "Native factory resolves custom type and required research");
            Check(FlowElement.Create("Mine").ElementType == "Mine", "Original native factory still resolves original game buildings");
        }

        void PrepareWorkspacePlacement()
        {
            Check(FlowElement.GetBuildableTypes().Count(n => n.ElementType == RelayId) == 1, "Research unlock exposes custom type in native build catalog");
            workspaceKeyboard = InputSystem.AddDevice<Keyboard>();
            workspaceKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(workspaceKeyboard, new KeyboardState());
            InputSystem.Update();
            GameUI.Instance.ToggleBuyMenu(true);
            var menu = FindObjectsByType<ElementBuyMenu>(FindObjectsSortMode.None).Single(n => n.isActiveAndEnabled);
            menu.ShowCategory(ElementCategory.Utility);
            var row = menu.GetComponentsInChildren<BuyMenuRow>().Last(r => TechTreeTabs.Field<FlowElement>(r, "_element").ElementType == RelayId);
            Check(TechTreeTabs.Field<TMPro.TMP_Text>(row, "_label").text == "Example Relay", "Native buy menu renders the workspace node's name");
            workspaceGold = GamePlayer.Current.GetCurrency(Currency.Gold);
            workspaceCount = GamePlayer.Current.ActiveFlow.Elements.Count;
            ExecuteEvents.Execute(row.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            relayGhost = TechTreeTabs.Field<WorldFlowGhost>(WorldFlowManager.Instance, "_currentGhost");
            Check(relayGhost && WorldFlowManager.Instance.BlueprintActive, "Native buy-row click starts the actual blueprint placement ghost");
            Check(((RectTransform)relayGhost.transform).sizeDelta == relayType.Definition.Size, "Native blueprint uses registered node dimensions");
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(-640, 192));
            relayA = (WorkspaceNode)GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == workspaceCount + 1 && GamePlayer.Current.GetCurrency(Currency.Gold) == workspaceGold - 20, "Native placement creates a saved model and charges its purchase cost");
            Check(relayA.Position == new Vector2Int(-640, 192) && relayA.World && relayA.World.Element == relayA, "Placed custom model is bound to the game's WorldFlowElement");
            GameUI.Instance.ToggleBuyMenu(false);
            relayGhost = relayType.BeginPlacement();
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(384, 192));
            relayB = (WorkspaceNode)GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(!ReferenceEquals(relayA, relayB) && relayA.Guid != relayB.Guid && GamePlayer.Current.GetCurrency(Currency.Gold) == workspaceGold - 40, "API placement uses the same native purchase flow for a second independent node");
        }

        void PlaceWorkspaceGhost(WorldFlowGhost ghost, Vector2Int position)
        {
            ghost.OnDragDropMove(position, Vector2Int.zero);
            Physics2D.SyncTransforms();
            AccessTools.Method(typeof(WorldFlowGhost), "Update").Invoke(ghost, null);
            DragDropManager.Instance.ClearDragDrop();
            ghost.OnDragDropEnd();
        }

        void ExerciseWorkspace()
        {
            var world = relayA.World;
            var template = TechTreeTabs.Field<WorldFlowElement>(WorldFlowManager.Instance, "_defaultPrefab");
            Check(world.GetType() == template.GetType() && world.BackgroundSprite == template.BackgroundSprite, "Workspace node uses the game's actual generic building prefab and background");
            Check(world.GetComponentInChildren<WorldFlowLabel>(true) && world.Collider && world.Collider.Element == world, "Native drag label and collision component remain attached");
            Check(relayA.Inputs[0].World && relayA.Outputs[0].World && relayA.Inputs[0].World.IO == relayA.Inputs[0], "Native input/output widgets bind to the mod's port models");
            var before = GamePlayer.Current.ActiveFlow.Elements.Count;
            var gold = GamePlayer.Current.GetCurrency(Currency.Gold);
            var blocked = relayType.BeginPlacement();
            PlaceWorkspaceGhost(blocked, relayA.Position);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == before && GamePlayer.Current.GetCurrency(Currency.Gold) == gold, "Native collision rejection prevents overlapping purchases without charging currency");
            WorldFlowManager.Instance.StopBlueprintMode();
            DragDropManager.Instance.ClearDragDrop();
            var label = relayA.World.GetComponentInChildren<WorldFlowLabel>(true);
            var start = relayA.Position;
            AccessTools.Method(typeof(WorldFlowLabel), "_startDragDrop").Invoke(label, new object[] { false });
            label.OnDragDropMove(start + new Vector2Int(64, 64), Vector2Int.zero);
            Check(relayA.Position == start + new Vector2Int(64, 64), "Workspace node moves through native label dragging and snapping");
            label.OnDragDropCancel();
            Check(relayA.Position == start, "Native drag cancellation restores workspace position");
            relayPipe = new FlowPipe();
            relayPipe.Connect(relayA.Outputs[0], relayB.Inputs[0]);
            relayPipe.Nodes.Add(new Vector2Int(0, 152));
            WorldFlowManager.Instance.CreateNewPipe(relayPipe);
            Check(relayPipe.World && relayA.Outputs[0].ConnectedPipe == relayPipe && relayB.Inputs[0].ConnectedPipe == relayPipe, "Native pipe renderer and models connect registered workspace ports");
            int nativeTransfers = 0;
            Action<FlowItemTransferSnapshot> onTransfer = transfer =>
            {
                if (transfer.Pipe.OutputElementGuid == relayA.Guid && transfer.ItemTypeId == "iron_ore")
                    nativeTransfers++;
            };
            FoundationApi.Flow.ItemTransferred += onTransfer;
            var item = new InventoryItem("iron_ore");
            Check(relayA.Inputs[0].InputItem(item) && !relayA.Inputs[0].InputItem(new InventoryItem("iron_ore")), "Native input buffer enforces its declared capacity");
            relayA.Update(.1f);
            relayPipe.Update(.1f);
            relayB.Update(.1f);
            Check(ReferenceEquals(relayB.Outputs[0].NextItem, item) && !relayA.Inputs[0].HasBuffer && !relayA.Outputs[0].HasBuffer, "Separate plugin behavior transfers the original item through native pipe flow");
            Check(nativeTransfers == 1, "Live native WorldFlowPipe publishes a real successful item-transfer event");
            Check(FoundationApi.IO.GetMeasuredThroughput(relayPipe).TotalTransfers == 1, "Measured native pipe transfer metrics match real observed item transfers");
            FoundationApi.Flow.ItemTransferred -= onTransfer;
            relayA.Outputs[0].BufferedItems.Add(new InventoryItem("iron_ore"));
            relayPipe.Update(.1f);
            relayA.Outputs[0].BufferedItems.Add(new InventoryItem("iron_ore"));
            relayPipe.Update(.1f);
            Check(relayA.Outputs[0].HasBuffer && relayB.Inputs[0].HasBuffer, "Native backpressure retains buffered items when destination is full");
            relayA.GetType().GetField("Transfers").SetValue(relayA, 7);
            var copy = (WorkspaceNode)relayA.CreateCopy();
            Check(copy.Guid != relayA.Guid && (int)copy.GetType().GetField("Transfers").GetValue(copy) == 7 && !copy.Inputs[0].HasBuffer && !copy.Outputs[0].HasBuffer, "Native copy creates a new identity and invokes external CopySettingsTo without copying inventory");
            var json = GamePlayer.Current.ActiveFlow.ToJson();
            var restored = FlowDiagram.FromJson(json);
            var restoredA = restored.Elements.Single(e => e.Guid == relayA.Guid);
            var restoredB = restored.Elements.Single(e => e.Guid == relayB.Guid);
            Check(restoredA is WorkspaceNode && restoredA.Position == relayA.Position && (int)restoredA.GetType().GetField("Transfers").GetValue(restoredA) == 7, "Native save round trip restores custom model, position, GUID and mod-owned state");
            Check(restoredA.Outputs[0].HasBuffer && restoredB.Inputs[0].HasBuffer && restoredB.Outputs[0].HasBuffer, "Native serializer restores workspace port inventories");
            Check(restored.Pipes.Any(p => p.ConnectedOutput.Parent == restoredA && p.ConnectedInput.Parent == restoredB && p.Nodes.SequenceEqual(relayPipe.Nodes)), "Native serializer reconnects workspace pipes and bends by saved GUID/port index");
            var player = GamePlayer.Current;
            try
            {
                var saved = GamePlayer.FromJson(player.ToJson());
                Check(saved.ActiveFlow.Elements.Count(e => e.ElementType == RelayId) == 2, "Full kingdom JSON round trip restores registered workspace instances");
            }
            finally
            {
                GamePlayer.Current = player;
            }

            relayGhost = WorldFlowManager.Instance.ShowPurchaseBlueprint(relayA, false);
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(384, -256));
            var placedCopy = GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == before + 1 && (int)placedCopy.GetType().GetField("Transfers").GetValue(placedCopy) == 7, "Native copy blueprint places the consumer's settings into another independent workspace node");
        }

        void FinishWorkspace(string directory)
        {
            var camera = TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera");
            WorldFlowCamera.Instance.SetZoomLevel(3);
            camera.transform.position = new Vector3(0, 0, -10);
            GameUI.Instance.ToggleBuyMenu(false);
            Capture(camera, Path.Combine(directory, "foundation-workspace.png"));
            var snapshot = FoundationApi.WorkspaceNodes.Nodes;
            int registeredCount = snapshot.Count;
            var saved = relayB.ToJson();
            relayType.Dispose();
            relayType.Dispose();
            Check(!relayType.IsRegistered && snapshot.Count == registeredCount && snapshot.Any(n => n.Id == RelayId) && FoundationApi.WorkspaceNodes.Nodes.Count == registeredCount - 1 && !FoundationApi.WorkspaceNodes.Nodes.Any(n => n.Id == RelayId), "Disposal removes building type from palette while keeping stable registry snapshots");
            Check(!FlowElement.GetBuildableTypes().Any(e => e.ElementType == RelayId) && relayB.World, "Disposed type is hidden from purchases while placed nodes stay intact");
            Check(FlowElement.FromJson(saved, new FlowDiagram()).ElementType == RelayId, "Inactive type remains loadable for existing saved instances during the session");
            Throws<ObjectDisposedException>(() => relayType.BeginPlacement(), "Disposed registration cannot start new placement");
            var replacement = FoundationApi.RegisterWorkspaceNode(relayType.Definition);
            relayType.Dispose();
            Check(replacement.IsRegistered && FoundationApi.WorkspaceNodes.Nodes.Count == registeredCount, "Stale registration disposal cannot unregister a replacement");
            WorldFlowManager.Instance.RemoveElement(relayA);
            Check(!GamePlayer.Current.ActiveFlow.Elements.Contains(relayA) && !GamePlayer.Current.ActiveFlow.Pipes.Contains(relayPipe) && relayB.Inputs[0].ConnectedPipe == null, "Native removal disconnects attached workspace pipes without deleting other nodes");
            if (workspaceKeyboard != null)
                InputSystem.RemoveDevice(workspaceKeyboard);
        }
    }
}
