using System;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using UnityEngine;
using RecipeElement = Assets.Source.Flow.Elements.RecipeElement;
using TimerNode = Assets.Source.Flow.Elements.Timer;
using MachineFilter = Assets.Source.Flow.Elements.Filter;

namespace KingdomFoundation.Machines
{
    /// <summary>Routes operations to actual Kingdom Inc implementations; does not simulate machines.</summary>
    public sealed class MachineOperations
    {
        private MachineOperationResult Guard(FlowElement el)
        {
            MachineThread.Verify();
            if (el == null || !MachineThread.IsPlaced(el)) return MachineOperationResult.Reject("Machine must belong to the active native kingdom.");
            if (!el.World) return MachineOperationResult.Reject("Native machine view is unavailable; operation requires loaded workspace.");
            return null;
        }
        private static MachineOperationResult Call(Action action)
        {
            try { action(); return MachineOperationResult.Ok(); }
            catch (Exception ex) { MachineRuntime.ReportCallbackError("Native machine operation", ex); return MachineOperationResult.Reject(ex.Message); }
        }
        /// <summary>Sets recipe.</summary>
        public MachineOperationResult SetRecipe(RecipeElement machine, CraftingRecipe recipe)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (recipe == null) return MachineOperationResult.Reject("Select a native recipe; no recipe removal supported by the game UI path.");
            if (!machine.CanCraft(recipe)) return MachineOperationResult.Reject("Native machine cannot craft this recipe.");
            bool available = false;
            foreach (var candidate in machine.GetAvailableRecipes()) if (ReferenceEquals(candidate, recipe)) { available = true; break; }
            if (!available) return MachineOperationResult.Reject("Recipe is not available/unlocked for this machine.");
            var view = machine.World.GetComponent<WorldFlowRecipe>();
            if (!view) return MachineOperationResult.Reject("WorldFlowRecipe component not found.");
            return Call(() => view.SetRecipe(recipe));
        }
        /// <summary>Sets resource.</summary>
        public MachineOperationResult SetResource(ResourceElement machine, ResourceItemType resource)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (resource == null) return MachineOperationResult.Reject("Null resource is not supported by the game's selection UI.");
            bool available = false;
            foreach (var r in machine.GetAvailableResources()) if (ReferenceEquals(r,resource)) { available = true; break; }
            if (!available) return MachineOperationResult.Reject("Resource not available or not valid for this machine.");
            var view = machine.World.GetComponent<WorldFlowResource>();
            if (!view) return MachineOperationResult.Reject("WorldFlowResource component not found.");
            return Call(() => view.SetResource(resource));
        }
        /// <summary>Sets forest resource.</summary>
        public MachineOperationResult SetForestResource(Forest machine, ForestItemType resource)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (resource == null || !resource.IsUnlocked) return MachineOperationResult.Reject("Native forest resource is null or not unlocked.");
            var view = machine.World.GetComponent<WorldFlowForest>();
            if (!view) return MachineOperationResult.Reject("WorldFlowForest component not found.");
            // The game's own resource selection UI owns unlock filtering.
            return Call(() => view.SetResource(resource));
        }
        /// <summary>Sets filter.</summary>
        public MachineOperationResult SetFilter(MachineFilter machine, ItemType type)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            // Delegate to Foundation's existing patch/event-aware API, not a parallel implementation.
            return Call(() => KingdomFoundation.Api.FoundationApi.Machines.SetFilter(machine, type));
        }
        public MachineOperationResult SetSwitch(TrackSwitch machine, bool activated)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            return Call(() =>
            {
                machine.SwitchPosition = activated;
                var control = machine.World.GetComponent<WorldFlowTrackSwitch>();
                if (control) control.SendMessage("UpdateSprite", SendMessageOptions.DontRequireReceiver);
            });
        }
        /// <summary>Performs the press button operation.</summary>
        public MachineOperationResult PressButton(SingleButton machine, int times = 1)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (times < 1 || times > 10) return MachineOperationResult.Reject("Press count must be between 1 and native UI maximum (10).");
            return Call(() => machine.ClickCount = Math.Min(10, Math.Max(0,machine.ClickCount) + times));
        }
        public MachineOperationResult SetTimer(TimerNode machine, float seconds)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return MachineOperationResult.Reject("Timer interval must be finite and positive.");
            if (!GamePlayer.Current.HasTech("outfitter_timeout")) return MachineOperationResult.Reject("Native timer configuration requires outfitter_timeout research.");
            return Call(() => { machine.MaxTime = seconds; machine.TimeLeft = seconds; });
        }
        /// <summary>Sets refiner mana.</summary>
        public MachineOperationResult SetRefinerMana(Refiner machine, bool useMana)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (GamePlayer.Current == null || !GamePlayer.Current.HasTech("refiner_mana"))
                return MachineOperationResult.Reject("Native refiner_mana research is required.");

            // The installed game exposes Refiner.UseMana as read-only. Do not try to
            // bypass the getter or change backing state. Use the game's own UI action.
            if (machine.UseMana == useMana) return MachineOperationResult.Ok();
            var view = machine.World.GetComponent<WorldFlowRefiner>();
            if (!view) return MachineOperationResult.Reject("Native WorldFlowRefiner control is unavailable.");

            var result = Call(view.ToggleUseMana);
            if (!result.Succeeded) return result;
            return machine.UseMana == useMana
                ? result
                : MachineOperationResult.Reject("Native refiner control did not apply the requested mana setting.");
        }
        /// <summary>Sets stockpile capacity.</summary>
        public MachineOperationResult SetStockpileCapacity(Stockpile machine, int capacity)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (!GamePlayer.Current.HasTech("stockpile_capacity")) return MachineOperationResult.Reject("Native stockpile_capacity research is required.");
            if (capacity < Stockpile.BaseCapacity || capacity < machine.Contents.Count) return MachineOperationResult.Reject("Capacity below native base or stored item count.");
            return Call(() => machine.Capacity = capacity);
        }
        public MachineOperationResult ExpandPorts(FlowElement machine)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            // Respect game's UI cap (4). The native entity handles resizing and IO visuals.
            if (machine is Collector c)
                return c.InputCount < 4 ? Call(c.AddInputSlot) : MachineOperationResult.Reject("Native collector input limit is 4.");
            if (machine is Distributor d)
                return d.OutputCount < 4 ? Call(d.AddOutputSlot) : MachineOperationResult.Reject("Native distributor output limit is 4.");
            if (machine is Manifold m)
                return m.InputCount < 4 ? Call(m.AddInputSlot) : MachineOperationResult.Reject("Native manifold input limit is 4.");
            return MachineOperationResult.Reject("This native machine has no expandable slots.");
        }
        /// <summary>Performs the collapse ports operation.</summary>
        public MachineOperationResult CollapsePorts(FlowElement machine)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (machine is Collector c) return Call(c.Collapse);
            if (machine is Distributor d) return Call(d.Collapse);
            if (machine is Manifold m) return Call(m.Collapse);
            return MachineOperationResult.Reject("This native machine has no collapsible slots.");
        }
        /// <summary>Sets portal direction.</summary>
        public MachineOperationResult SetPortalDirection(Portal machine, bool isOutput)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            // Port layout changes must go through the native method that updates WorldFlowElement IO.
            return Call(() => machine.SetOutput(isOutput));
        }
        public MachineOperationResult SetArmoryOutput(Armory machine, ItemType type)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            // Existing pipes might need invalidation when changing direction. Refuse rather than orphan connections.
            foreach (var port in machine.IO) if (port.ConnectedPipe != null)
                return MachineOperationResult.Reject("Disconnect all armory pipes before changing output mode.");
            return Call(() => { machine.SetOutputType(type); machine.World.UpdateIO(); machine.World.UpdateIcon(); });
        }
        /// <summary>Performs the start manual craft operation.</summary>
        public MachineOperationResult StartManualCraft(FlowElement machine)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (!machine.CanManualCraft()) return MachineOperationResult.Reject("Native machine cannot manually craft now.");
            return Call(machine.DoManualCraft);
        }
        public MachineOperationResult SetQuest(QuestBoard machine, Assets.Source.Adventure.Quest quest)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (quest == null || !quest.IsUnlocked) return MachineOperationResult.Reject("Null or locked quest not supported.");
            return Call(() => { machine.UpdateQuest(quest);machine.World.UpdateIO(); });
        }
        /// <summary>Sets dungeon.</summary>
        public MachineOperationResult SetDungeon(Dungeon machine, Assets.Source.Adventure.DungeonType dungeon)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (dungeon == null || !GamePlayer.Current.UnlockedDungeons.Contains(dungeon.Identifier)) return MachineOperationResult.Reject("Native dungeon is null or not unlocked.");
            return Call(() => machine.SetDungeonType(dungeon));
        }
        public MachineOperationResult SetRequisitionContract(RequisitionOffice machine, int index)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            // Native UI restricts the valid index to the current player contract catalog.
            if (index < 0 || index >= GamePlayer.Current.RequisitionTypes.Count)
                return MachineOperationResult.Reject("Contract index is not in native current catalog.");
            return Call(() => machine.SetSelectedContract(index));
        }
        /// <summary>Native item-stat comparator. Uses the same public fields and
        /// view updates as ItemAppraiserPopup.UpdateSettings; never evaluates items itself.</summary>
        public MachineOperationResult SetAppraiser(Appraiser machine, AppraiserStat? stat, AppraiserType comparison, int amount)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (!Enum.IsDefined(typeof(AppraiserType), comparison)) return MachineOperationResult.Reject("Unknown native appraiser comparison.");
            if (stat.HasValue && !Enum.IsDefined(typeof(AppraiserStat), stat.Value))
                return MachineOperationResult.Reject("Unknown native appraiser stat.");
            var view = machine.World.GetComponent<WorldFlowAppraiser>();
            if (!view) return MachineOperationResult.Reject("Native WorldFlowAppraiser control unavailable.");
            return Call(() =>
            {
                machine.StatAppraised = stat;
                machine.AppraiseType = comparison;
                machine.StatAmount = amount;
                machine.World.UpdateIcon();
                view.UpdateText();
            });
        }

        /// <summary>Native currency comparator. Currency units and Gold conversion
        /// are determined by Accountant.AppraiseItem, never by Foundation.</summary>
        public MachineOperationResult SetAccountant(Accountant machine, Currency? currency, AppraiserType comparison, long amount)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (!Enum.IsDefined(typeof(AppraiserType), comparison)) return MachineOperationResult.Reject("Unknown native accountant comparison.");
            if (currency.HasValue && !Enum.IsDefined(typeof(Currency), currency.Value))
                return MachineOperationResult.Reject("Unknown native currency.");
            var view = machine.World.GetComponent<WorldFlowAccountant>();
            if (!view) return MachineOperationResult.Reject("Native WorldFlowAccountant control unavailable.");
            return Call(() =>
            {
                machine.CurrType = currency;
                machine.AppraiseType = comparison;
                machine.CurrAmount = amount;
                machine.World.UpdateIcon();
                view.UpdateText();
            });
        }

        /// <summary>Uses the game's WarEffort selection view, restricted to native
        /// WarProgress entries presently offered to the player.</summary>
        public MachineOperationResult SetWarType(WarEffort machine, WarType war)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (war == null) return MachineOperationResult.Reject("Choose a war from the native current-player list.");
            bool exists = false;
            foreach (var available in GamePlayer.Current.Wars)
                if (ReferenceEquals(available.War, war)) { exists = true; break; }
            if (!exists) return MachineOperationResult.Reject("War is not available in the game's current war list.");
            var view = machine.World.GetComponent<WorldFlowWarEffort>();
            if (!view) return MachineOperationResult.Reject("Native WorldFlowWarEffort control unavailable.");
            return Call(() => view.SetWarType(war));
        }

        /// <summary>Native collector selection logic. Game Collector.NextInput
        /// controls the rotation; no Foundation scheduler or balancing policy.</summary>
        public MachineOperationResult AdvanceCollector(Collector machine)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            return Call(machine.NextInput);
        }

        /// <summary>Configures the actual Outfitter.Timeout field used by
        /// WorldFlowOutfitter.UpdateSlider (value / 2f). The game saves this
        /// setting in Outfitter.DataToJson. No Foundation timer is created.</summary>
        public MachineOperationResult SetOutfitterTimeout(Outfitter machine, float seconds)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return MachineOperationResult.Reject("Native outfitter timeout must be finite and nonnegative.");
            if (!GamePlayer.Current.HasTech("outfitter_timeout"))
                return MachineOperationResult.Reject("Native outfitter_timeout research is required.");
            // The installed game exposes this value as a writable native field.
            // Its own WorldFlowOutfitter reads the same field when opening UI.
            var view = machine.World.GetComponent<WorldFlowOutfitter>();
            if (!view) return MachineOperationResult.Reject("Native WorldFlowOutfitter control unavailable.");
            return Call(() => machine.Timeout = seconds);
        }

        /// <summary>Routes through the preexisting validated game-native channel
        /// method, which calls ClearOutput before switching active portal outputs.</summary>
        public MachineOperationResult SetPortalChannel(Portal machine, int channel, int index)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            return Call(() => KingdomFoundation.Api.FoundationApi.Machines.SetPortalChannel(machine, channel, index));
        }

        /// <summary>Native armory channel bounds are enforced by the existing
        /// bridge using ArmoryContents.ChannelCount.</summary>
        public MachineOperationResult SetArmoryChannel(Armory machine, int channel)
        {
            var bad = Guard(machine); if (bad != null) return bad;
            return Call(() => KingdomFoundation.Api.FoundationApi.Machines.SetArmoryChannel(machine, channel));
        }

        public MachineOperationResult Connect(FlowOutput output, FlowInput input)
        {
            MachineThread.Verify();
            if (output == null || input == null) return MachineOperationResult.Reject("Both native ports are required.");
            if (!MachineThread.IsPlaced(output.Parent) || !MachineThread.IsPlaced(input.Parent)) return MachineOperationResult.Reject("Ports must belong to placed machines.");
            if (output.ConnectedPipe != null || input.ConnectedPipe != null) return MachineOperationResult.Reject("Disconnect occupied ports first.");
            var manager = WorldFlowManager.Instance;
            if (!manager) return MachineOperationResult.Reject("Native WorldFlowManager is not running.");
            var flow = manager.CurrentFlow;
            if (!flow.Elements.Contains(output.Parent) || !flow.Elements.Contains(input.Parent))
                return MachineOperationResult.Reject("The native editor only creates pipes on the currently visible flow.");
            if (output.Parent == input.Parent) return MachineOperationResult.Reject("Self-pipe connections are not supported by this API.");
            if (!output.World || !input.World) return MachineOperationResult.Reject("Native port views are not initialized.");
            return Call(() =>
            {
                var pipe = new FlowPipe(); pipe.Connect(output,input);
                try { manager.CreateNewPipe(pipe); }
                catch
                {
                    if (flow.Pipes.Contains(pipe)) manager.DisconnectPipe(pipe,flow);
                    else
                    {
                        if (ReferenceEquals(output.ConnectedPipe,pipe)) output.ConnectedPipe=null;
                        if (ReferenceEquals(input.ConnectedPipe,pipe)) input.ConnectedPipe=null;
                    }
                    throw;
                }
            });
        }
        /// <summary>Performs the disconnect operation.</summary>
        public MachineOperationResult Disconnect(FlowPipe pipe)
        {
            MachineThread.Verify();
            if (pipe == null) return MachineOperationResult.Reject("Native pipe required.");
            var manager = WorldFlowManager.Instance;
            if (!manager || GamePlayer.Current == null) return MachineOperationResult.Reject("Native workspace unavailable.");
            foreach (var flow in GamePlayer.Current.Flows)
                if (flow.Pipes.Contains(pipe)) return Call(() => manager.DisconnectPipe(pipe,flow));
            return MachineOperationResult.Reject("Pipe does not belong to the current kingdom.");
        }
    }
}
