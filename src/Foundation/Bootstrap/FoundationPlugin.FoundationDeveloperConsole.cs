using System;
using System.Linq;
using Assets.Source.Player;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        ConfigEntry<bool> enableDeveloperConsole;
        bool consoleOpen;
        Rect consoleWindow = new Rect(35, 35, 720, 590);
        Vector2 consoleScroll;
        int consoleTab;
        int selectedElement;
        float nextSignalPoll;
        private static readonly string[] ConsoleTabs = { "Overview", "Workspace", "Signals", "Events", "Diagnostics" };

        private void SetupDeveloperConsole()
        {
            enableDeveloperConsole = Config.Bind("Developer", "EnableConsole", false,
                "Enable the in-game Foundation inspector. Press F8 to show/hide. Disabled by default.");
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
            if (enableDeveloperConsole?.Value != true || !consoleOpen) return;
            consoleWindow = GUI.Window(765142, consoleWindow, DrawDeveloperWindow, "Foundation Developer Inspector [F8]");
        }
        void DrawDeveloperWindow(int id)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < ConsoleTabs.Length; i++)
                if (GUILayout.Toggle(consoleTab == i, ConsoleTabs[i], GUI.skin.button)) { if (consoleTab != i) consoleScroll = Vector2.zero; consoleTab = i; }
            if (GUILayout.Button("X", GUILayout.Width(26))) consoleOpen = false;
            GUILayout.EndHorizontal();
            consoleScroll = GUILayout.BeginScrollView(consoleScroll);
            try
            {
                switch (consoleTab)
                {
                    case 0: DrawOverview(); break;
                    case 1: DrawWorkspace(); break;
                    case 2: DrawSignals(); break;
                    case 3: DrawEvents(); break;
                    case 4: DrawDiagnostics(); break;
                }
            }
            catch (Exception ex) { GUILayout.Label("Inspector error: " + ex.Message); }
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
            if (player == null || player.Flows.Count == 0) { GUILayout.Label("No kingdom loaded."); return; }
            var flow = FoundationApi.World.ActiveFlow;
            GUILayout.Label("Flow: " + player.CurrentFlow + " (" + (flow.Label ?? "unnamed") + ") — " + flow.Elements.Count + " elements, " + flow.Pipes.Count + " pipes");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", GUILayout.Width(100))) selectedElement = Math.Max(0, selectedElement - 1);
            if (GUILayout.Button("Next", GUILayout.Width(100))) selectedElement = Math.Min(flow.Elements.Count - 1, selectedElement + 1);
            GUILayout.EndHorizontal();
            if (flow.Elements.Count == 0) return;
            selectedElement = Math.Max(0, Math.Min(selectedElement, flow.Elements.Count - 1));
            var element = flow.Elements[selectedElement];
            var snapshot = FoundationApi.Flow.Inspect(element);
            GUILayout.Label("Element [" + selectedElement + "] " + snapshot.Label + " / " + snapshot.TypeId);
            GUILayout.Label("GUID: " + snapshot.Guid);
            GUILayout.Label("Position: " + snapshot.Position + "  Size: " + snapshot.Size);
            GUILayout.Label("Progress: " + snapshot.Progress.ToString("0.000") + "  Stack: " + snapshot.StackSize);
            foreach (var port in snapshot.Inputs.Concat(snapshot.Outputs))
                GUILayout.Label(" " + port.Direction + "[" + port.Index + "] item=" + port.AcceptedItemTypeId +
                    " buffer=" + port.BufferedCount + "/" + port.BufferCapacity + " connected=" + port.IsConnected);
            foreach (var pipe in flow.Pipes.Where(p => p.ConnectedOutput?.Parent == element || p.ConnectedInput?.Parent == element))
            {
                var stats = FoundationApi.IO.GetMeasuredThroughput(pipe);
                GUILayout.Label(" Pipe " + pipe.ConnectedOutput?.Parent?.Guid + " -> " + pipe.ConnectedInput?.Parent?.Guid +
                    "  transfers=" + stats.TotalTransfers + "  observed/s=" + stats.AverageTransfersPerSecond.ToString("0.0") + " (10s)");
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
                    try { value = property.Read()?.ToString() ?? "null"; }
                    catch (Exception ex) { value = "ERROR: " + ex.Message; }
                    GUILayout.Label("    " + property.Name + " [" + property.Id + "] " + property.ValueType.Name + " = " + value + (property.CanWrite ? " (RW)" : " (RO)"));
                }
            }
            if (FoundationApi.Signals.Sources.Count == 0) GUILayout.Label("No mod has registered a signal source.");
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
    }
}
