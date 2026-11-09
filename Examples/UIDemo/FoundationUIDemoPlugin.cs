using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using BepInEx;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using UnityEngine;

namespace KingdomFoundation.Examples
{
    /// <summary>Development-only panel exercise; not part of the public player release.</summary>
    [BepInPlugin("urakka.foundation.example.ui", "Foundation UI Verification Demo", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FoundationUIDemoPlugin : BaseUnityPlugin
    {
        private IFoundationPanelModContext panels;
        private IModBarRegistration openButton;
        private IModBarRegistration machineButton;
        private int clickCount;
        private bool enabledOption = true;
        private float slider = .5f;
        private double numeric = 5;
        private string text = "Hello";
        private int chosen;
        private int color;
        private SignalSelection signal;
        private List<string> order = new List<string> { "First", "Second", "Third" };

        private void Awake()
        {
            panels = FoundationApi.Panels.ForMod("urakka.foundation.example.ui");
            panels.Register("controls", "Foundation UI - Control Gallery", BuildGallery);
            openButton = FoundationApi.ModBar.Register(new ModBarButton(
                "urakka.foundation.example.ui", "open", "Test UI", () => panels.Open("controls"), 125));
            machineButton = FoundationApi.ModBar.Register(new ModBarButton(
                "urakka.foundation.example.ui", "machine", "Machine UI", OpenMachine, 150, order: 1));
            Logger.LogInfo("[FOUNDATION UI DEMO] Control gallery registered for the unified Foundation DLL.");
        }

        private void BuildGallery(IFoundationPanelBuilder ui)
        {
            ui.Header("Reusable native-themed controls");
            ui.Text("This is an independent mod consuming Foundation.Panels.");
            ui.Button("Action", () => { clickCount++; Logger.LogInfo("[FOUNDATION UI DEMO] Action clicked"); ui.Rebuild(); },
                "Button callback on Unity's main thread");
            ui.LiveText("Action count", () => clickCount.ToString());
            ui.Toggle("Enable feature", enabledOption, next => enabledOption = next, "Toggle with native checkbox");
            ui.Slider("Fine adjustment", 0, 1, slider, .05f, next => slider = next);
            ui.Number("Numeric input", numeric, -100, 100, .5, next => numeric = next);
            ui.Input("Text input", text, next => text = next);
            ui.Dropdown("Native dropdown", new[] { "Entry A", "Entry B", "Entry C" }, chosen, next => chosen = next);
            ui.Search("Search / lookup all options", new[] { "Amber", "Blue", "Crimson", "Dandelion", "Emerald", "Fuchsia" }, color,
                next => { color = next; ui.Rebuild(); });
            ui.Colors("Rectangular swatches", new[] { Color.red, Color.green, Color.blue, Color.yellow },
                Mathf.Clamp(color, 0, 3), next => { color = next; ui.Rebuild(); });
            ui.Signal("Signal source and property", signal, next => { signal = next; ui.Rebuild(); });
            ui.Section("optional", "Collapsible group", panel =>
            {
                panel.Text("Sections persist their collapsed state per mod panel.");
                panel.Columns(2, (column, nested) => nested.Text("Column " + (column + 1)));
            });
            ui.Tabs("gallery", new[] { "General", "Advanced" }, (index, panel) =>
            {
                if (index == 0) panel.Text("The active tab persists locally.");
                else panel.Property(new FoundationUiProperty("value", "Dynamic numeric property", typeof(double),
                    () => numeric, next => numeric = (double)next, minimum: -50, maximum: 50, step: .25));
            });
            ui.Reorder("Drag rows to reorder", order, (from, to) =>
            {
                var item = order[from]; order.RemoveAt(from); order.Insert(to, item);
                Logger.LogInfo("[FOUNDATION UI DEMO] Reordered " + from + " -> " + to);
            });
            ui.Confirm("Run confirmation callback?", () => Logger.LogInfo("[FOUNDATION UI DEMO] Confirm accepted"));
        }

        private void OpenMachine()
        {
            if (GamePlayer.Current == null)
            {
                Logger.LogWarning("[FOUNDATION UI DEMO] Load a kingdom first.");
                return;
            }
            var all = FoundationMachineApi.Registry.OnFlow(GamePlayer.Current.CurrentFlow);
            var machine = all.FirstOrDefault(m => m is TrackSwitch) ?? all.FirstOrDefault();
            if (machine == null) { Logger.LogWarning("[FOUNDATION UI DEMO] Place a native machine first."); return; }
            if (!FoundationApi.Panels.OpenMachine(machine))
                Logger.LogWarning("[FOUNDATION UI DEMO] No matching machine panel.");
            else Logger.LogInfo("[FOUNDATION UI DEMO] Machine popup opened: " + machine.ElementType);
        }

        private void OnDestroy()
        {
            machineButton?.Dispose();
            openButton?.Dispose();
            panels?.Dispose();
        }
    }
}
