using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using UnityEngine;
using RecipeElement = Assets.Source.Flow.Elements.RecipeElement;
using TimerNode = Assets.Source.Flow.Elements.Timer;
using StockpileNode = Assets.Source.Flow.Elements.Stockpile;
using FilterNode = Assets.Source.Flow.Elements.Filter;

namespace KingdomFoundation
{
    /// <summary>Native-backed default editors. Consumer panels always outrank these by default.</summary>
    internal static class FoundationNativeMachinePanels
    {
        private const int DefaultPriority = -1000;
        private static readonly List<IFoundationPanelRegistration> registrations = new List<IFoundationPanelRegistration>();
        private static readonly MachineOperations operations = FoundationMachineApi.Operations;

        internal static void Install()
        {
            if (registrations.Count != 0) return;
            Add<FlowElement>("all", "Machine", Overview);
            Add<TrackSwitch>("switch", "Track Switch", Switch);
            Add<SingleButton>("button", "Button", PushButton);
            Add<TimerNode>("timer", "Timer", Timer);
            Add<Refiner>("refiner", "Refiner", Refiner);
            Add<RecipeElement>("recipe", "Crafting", Recipe);
            Add<ResourceElement>("resource", "Resource Producer", Resource);
            Add<Forest>("forest", "Forest", Forest);
            Add<FilterNode>("filter", "Filter", Filter);
            Add<StockpileNode>("stockpile", "Stockpile", Stockpile);
            Add<Collector>("collector", "Collector", PortExpander);
            Add<Distributor>("distributor", "Distributor", PortExpander);
            Add<Manifold>("manifold", "Manifold", PortExpander);
            Add<Portal>("portal", "Portal", Portal);
            Add<Armory>("armory", "Armory", Armory);
            Add<QuestBoard>("quest", "Quest Board", Quest);
            Add<Dungeon>("dungeon", "Dungeon", Dungeon);
            Add<RequisitionOffice>("requisition", "Requisition Office", Requisition);
        }

        private static void Add<T>(string id, string label, Action<IFoundationPanelBuilder> render) where T : FlowElement
        {
            registrations.Add(FoundationApi.Panels.Register(new FoundationPanelDefinition(
                FoundationPlugin.Id, "native-" + id, label, render, typeof(T), DefaultPriority)));
        }

        internal static void Uninstall()
        {
            for (int i = registrations.Count - 1; i >= 0; i--) registrations[i].Dispose();
            registrations.Clear();
        }

        private static void Overview(IFoundationPanelBuilder ui)
        {
            var machine = ui.Machine;
            if (machine == null) return;
            ui.Tabs("native-information", new[] { "Machine", "I/O", "Signals" }, (tab, builder) =>
            {
                if (tab == 0)
                {
                    builder.Text("Native type: " + machine.ElementType);
                    builder.LiveText("Progress", () => machine.Progress.ToString("P1"));
                    builder.LiveText("Craft speed", () => machine.Speed.ToString("0.##"));
                    builder.LiveText("Connected pipes", () => FoundationMachineApi.Registry.ConnectionsOf(machine).Count.ToString());
                }
                else if (tab == 1)
                {
                    builder.Text("Inputs: " + machine.Inputs.Count + "   Outputs: " + machine.Outputs.Count);
                    for (int i = 0; i < machine.Inputs.Count; i++)
                    {
                        int index = i;
                        builder.LiveText("Input " + i, () => machine.Inputs.Count > index ?
                            (machine.Inputs[index].ConnectedPipe == null ? "Disconnected" : "Connected") : "Removed");
                    }
                    for (int i = 0; i < machine.Outputs.Count; i++)
                    {
                        int index = i;
                        builder.LiveText("Output " + i, () => machine.Outputs.Count > index ?
                            (machine.Outputs[index].ConnectedPipe == null ? "Disconnected" : "Connected") : "Removed");
                    }
                }
                else
                {
                    builder.Text("Registered signal sources: " + FoundationApi.Signals.Sources.Count);
                    builder.Text("Signal values and properties are selected by a mod's own bindings.");
                }
            });
        }

        private static void Apply(IFoundationPanelBuilder ui, MachineOperationResult result)
        {
            if (result.Succeeded) ui.Rebuild();
            else ui.Message(result.Message);
        }

        private static void Switch(IFoundationPanelBuilder ui)
        {
            var switchNode = (TrackSwitch)ui.Machine;
            ui.Toggle("Route to output 0", switchNode.SwitchPosition,
                next => Apply(ui, operations.SetSwitch(switchNode, next)), "Change the native switch route.");
            Overview(ui);
        }

        private static void PushButton(IFoundationPanelBuilder ui)
        {
            var button = (SingleButton)ui.Machine;
            ui.LiveText("Queued presses", () => button.ClickCount.ToString());
            ui.Button("Press", () => Apply(ui, operations.PressButton(button)));
            Overview(ui);
        }

        private static void Timer(IFoundationPanelBuilder ui)
        {
            var timer = (TimerNode)ui.Machine;
            ui.LiveText("Time left", () => timer.TimeLeft.ToString("0.##") + "s");
            if (GamePlayer.Current != null && GamePlayer.Current.HasTech("outfitter_timeout"))
                ui.Number("Interval (seconds)", timer.MaxTime, .05, 100000, .05, next =>
                    Apply(ui, operations.SetTimer(timer, (float)next)));
            else ui.Text("Timer adjustment requires native outfitter_timeout research.");
            Overview(ui);
        }

        private static void Refiner(IFoundationPanelBuilder ui)
        {
            var refiner = (Refiner)ui.Machine;
            if (GamePlayer.Current != null && GamePlayer.Current.HasTech("refiner_mana"))
                ui.Toggle("Use mana", refiner.UseMana, next => Apply(ui, operations.SetRefinerMana(refiner, next)));
            else ui.Text("Mana configuration requires native refiner_mana research.");
            ui.LiveText("Last mana cost", () => refiner.LastManaCost.ToString());
            Overview(ui);
        }

        private static void Recipe(IFoundationPanelBuilder ui)
        {
            var machine = (RecipeElement)ui.Machine;
            ui.Text("Selected: " + (machine.Recipe?.Identifier ?? "(none)"));
            ui.Button("Choose recipe (native menu)", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowRecipe>() : null;
                if (!view) { ui.Message("Native recipe selector is unavailable."); return; }
                view.ShowRecipePopup();
            });
            if (machine.CanManualCraft())
                ui.Button("Craft manually", () => Apply(ui, operations.StartManualCraft(machine)));
            Overview(ui);
        }

        private static void Resource(IFoundationPanelBuilder ui)
        {
            var machine = (ResourceElement)ui.Machine;
            ui.Text("Selected: " + (machine.Resource?.GetDisplayName() ?? "(none)"));
            ui.LiveText("Production / sec", () => machine.OutputPerSecond.ToString("0.##"));
            ui.Button("Choose resource (native menu)", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowResource>() : null;
                if (view) view.ShowResourcePopup();
                else ui.Message("Native resource selector is unavailable.");
            });
            Overview(ui);
        }

        private static void Forest(IFoundationPanelBuilder ui)
        {
            var forest = (Forest)ui.Machine;
            ui.Text("Selected: " + (forest.SelectedResource?.GetDisplayName() ?? "(none)"));
            ui.LiveText("Output / sec", () => forest.OutputPerSecond.ToString("0.##"));
            ui.Button("Choose forest resource (native menu)", () =>
            {
                var view = forest.World ? forest.World.GetComponent<WorldFlowForest>() : null;
                if (view) view.OpenResourcePopup();
                else ui.Message("Native forest resource selector is unavailable.");
            });
            Overview(ui);
        }

        private static void Filter(IFoundationPanelBuilder ui)
        {
            var machine = (FilterNode)ui.Machine;
            ui.Text("Filtered item: " + (machine.FilterType?.GetDisplayName() ?? "(none)"));
            ui.Button("Choose filter item (native menu)", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowFilter>() : null;
                if (view) view.ToggleFilterSelect();
                else ui.Message("Native filter selector is unavailable.");
            });
            Overview(ui);
        }

        private static void Stockpile(IFoundationPanelBuilder ui)
        {
            var machine = (StockpileNode)ui.Machine;
            ui.LiveText("Stored", () => machine.Contents.Count.ToString());
            if (GamePlayer.Current != null && GamePlayer.Current.HasTech("stockpile_capacity"))
                ui.Number("Capacity", machine.Capacity, StockpileNode.BaseCapacity, 1000000, 1,
                    next => Apply(ui, operations.SetStockpileCapacity(machine, (int)next)));
            else ui.Text("Capacity upgrade requires stockpile_capacity research.");
            Overview(ui);
        }

        private static void PortExpander(IFoundationPanelBuilder ui)
        {
            var machine = ui.Machine;
            ui.Text("Input ports: " + machine.Inputs.Count + "   Output ports: " + machine.Outputs.Count);
            ui.Button("Add native I/O port", () => Apply(ui, operations.ExpandPorts(machine)));
            ui.Confirm("Collapse native ports? Connected ports may be affected.",
                () => Apply(ui, operations.CollapsePorts(machine)));
            Overview(ui);
        }

        private static void Portal(IFoundationPanelBuilder ui)
        {
            var machine = (Portal)ui.Machine;
            ui.Text("Channel: " + machine.Channel + " / " + machine.ChannelIndex);
            ui.Toggle("Output direction", machine.IsOutput,
                next => Apply(ui, operations.SetPortalDirection(machine, next)));
            ui.Button("Native portal settings", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowPortal>() : null;
                if (view) view.TogglePopup();
                else ui.Message("Native portal settings are unavailable.");
            });
            Overview(ui);
        }

        private static void Armory(IFoundationPanelBuilder ui)
        {
            var machine = (Armory)ui.Machine;
            ui.Text("Channel: " + machine.Channel);
            ui.Text("Output: " + (machine.OutputType?.GetDisplayName() ?? "(none)"));
            ui.Button("Native armory configuration", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowArmory>() : null;
                if (view) view.TogglePopup();
                else ui.Message("Native armory settings are unavailable.");
            });
            Overview(ui);
        }

        private static void Quest(IFoundationPanelBuilder ui)
        {
            var machine = (QuestBoard)ui.Machine;
            ui.Text("Adventurers: " + machine.Contents.Count);
            ui.Button("Choose quest (native menu)", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowQuestBoard>() : null;
                if (view) view.ToggleQuestPopup();
                else ui.Message("Native quest selector is unavailable.");
            });
            Overview(ui);
        }

        private static void Dungeon(IFoundationPanelBuilder ui)
        {
            var machine = (Dungeon)ui.Machine;
            ui.Button("Choose dungeon (native menu)", () =>
            {
                var view = machine.World ? machine.World.GetComponent<WorldFlowDungeon>() : null;
                if (view) view.OpenDungeonPopup();
                else ui.Message("Native dungeon selector is unavailable.");
            });
            Overview(ui);
        }

        private static void Requisition(IFoundationPanelBuilder ui)
        {
            var machine = (RequisitionOffice)ui.Machine;
            ui.LiveText("Stored value", () => machine.StoredValue.ToString());
            var types = GamePlayer.Current?.RequisitionTypes;
            if (types != null && types.Count > 0)
            {
                var labels = types.Select(t => t.GetDisplayName()).ToArray();
                ui.Search("Native contract", labels, Math.Max(0, machine.SelectedContract),
                    next => Apply(ui, operations.SetRequisitionContract(machine, next)));
            }
            Overview(ui);
        }
    }
}
