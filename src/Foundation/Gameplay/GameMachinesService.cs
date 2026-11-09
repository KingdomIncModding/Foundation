using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameMachinesService : IGameMachinesService
    {
        static void Active(FlowElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var player = GameWorldService.RequirePlayer();
            if (!player.Flows.Any(f => f.Elements.Contains(element)))
                throw new InvalidOperationException("Machine must be placed in a loaded kingdom.");
        }
        public IReadOnlyList<FlowElement> Catalog => Array.AsReadOnly(FlowElement.GetAllTypes().ToArray());
        public IReadOnlyList<FlowElement> BuildableCatalog => Array.AsReadOnly(FlowElement.GetBuildableTypes().ToArray());
        public IReadOnlyList<T> Find<T>() where T : FlowElement
            => Array.AsReadOnly(FoundationApi.World.FindElements(e => e is T).Cast<T>().ToArray());
        public bool ConfigureRecipe(RecipeElement machine, CraftingRecipe recipe)
        {
            Active(machine);
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (!machine.CanCraft(recipe)) return false;
            if (machine.Recipe == recipe) return true;
            // The game's view component owns disconnection/reconnection of pipes
            // whenever a recipe changes. Calling SetupRecipe directly here would
            // append duplicate ports and strand connections in the diagram.
            if (!machine.World) throw new InvalidOperationException("A live native machine view is required to safely change a placed recipe.");
            var view = machine.World.GetComponent<WorldFlowRecipe>();
            if (!view) throw new InvalidOperationException("Native recipe view is not available.");
            view.SetRecipe(recipe);
            return true;
        }
        public bool ConfigureForest(Forest machine, ForestItemType resource)
        {
            Active(machine);
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            var forest = GameWorldService.RequirePlayer().ForestSettings;
            if (forest == null || !forest.ItemUnlocked(resource)) return false;
            if (machine.SelectedResource == resource) return true;
            if (!machine.World) throw new InvalidOperationException("A live native forest view is required to change a placed resource.");
            var view = machine.World.GetComponent<WorldFlowForest>();
            if (!view) throw new InvalidOperationException("Native forest view is not available.");
            view.SetResource(resource);
            return true;
        }
        public void SetFilter(Filter machine, ItemType item) { Active(machine); machine.SetFilterType(item); }
        public void SetSwitch(TrackSwitch machine, bool enabled)
        {
            Active(machine);
            if (machine.SwitchPosition == enabled) return;
            machine.SwitchPosition = enabled;
            machine.UpdateTrackSwitch();
        }
        public void SetTimer(Timer machine, float seconds)
        {
            Active(machine);
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            machine.MaxTime = seconds;
            if (machine.TimeLeft > seconds) machine.TimeLeft = seconds;
        }
        public void SetAppraiser(Appraiser machine, AppraiserStat? stat, AppraiserType op, int threshold)
        {
            Active(machine);
            machine.StatAppraised = stat;
            machine.AppraiseType = op;
            machine.StatAmount = threshold;
        }
        public void AddDistributorOutput(Distributor machine) { Active(machine); machine.AddOutputSlot(); }
        public void AddManifoldInput(Manifold machine) { Active(machine); machine.AddInputSlot(); }
        public void AddCollectorInput(Collector machine) { Active(machine); machine.AddInputSlot(); }
        public void AdvanceCollector(Collector machine) { Active(machine); machine.NextInput(); }
        public void SetArmoryOutput(Armory machine, ItemType type) { Active(machine); machine.SetOutputType(type); }
        public void SetPortalOutput(Portal machine, bool output) { Active(machine); machine.SetOutput(output); }
        public void SetPortalChannel(Portal machine, int channel, int index)
        {
            Active(machine);
            if (channel < 0 || channel >= PortalContents.ChannelCount) throw new ArgumentOutOfRangeException(nameof(channel));
            if (index < 0 || index >= PortalContents.IndexCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (machine.Channel == channel && machine.ChannelIndex == index) return;
            // The native ClearOutput hands staged output to the previous channel
            // before the routing address is switched.
            if (machine.IsOutput) machine.ClearOutput();
            machine.Channel = channel;
            machine.ChannelIndex = index;
        }
        public void SetArmoryChannel(Armory machine, int channel)
        {
            Active(machine);
            if (channel < 0 || channel >= ArmoryContents.ChannelCount) throw new ArgumentOutOfRangeException(nameof(channel));
            machine.Channel = channel;
        }
        public void SelectQuest(QuestBoard machine, Quest quest)
        {
            Active(machine);
            if (quest != null && !quest.IsUnlocked) throw new InvalidOperationException("Quest has not been unlocked.");
            machine.UpdateQuest(quest);
        }
        public void SelectDungeon(Dungeon machine, DungeonType dungeon)
        {
            Active(machine);
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            if (!GameWorldService.RequirePlayer().UnlockedDungeons.Contains(dungeon.Identifier))
                throw new InvalidOperationException("Dungeon has not been unlocked.");
            machine.SetDungeonType(dungeon);
        }
        public void SelectRequisition(RequisitionOffice machine, int index)
        { Active(machine); machine.SetSelectedContract(index); }
        public void CollectShop(Shop machine)
        { Active(machine); if (machine.CanManualCraft()) machine.DoManualCraft(); }
        public void CollectRequisition(RequisitionOffice machine)
        { Active(machine); if (machine.CanManualCraft()) machine.DoManualCraft(); }
        public IReadOnlyList<CraftingRecipe> AvailableRecipes(RecipeElement machine)
        {
            Active(machine);
            return Array.AsReadOnly(machine.GetAvailableRecipes().ToArray());
        }
        public bool CanCraft(RecipeElement machine, CraftingRecipe recipe)
        {
            Active(machine);
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            return machine.CanCraft(recipe);
        }
        public Assets.Source.Player.CurrencyCollection CraftingCost(RecipeElement machine, CraftingRecipe recipe = null)
        {
            Active(machine);
            return machine.GetCraftingCost(recipe);
        }
        public IReadOnlyList<ResourceItemType> AvailableResources(ResourceElement machine)
        {
            Active(machine);
            return Array.AsReadOnly(machine.GetAvailableResources().ToArray());
        }
        public Assets.Source.Player.CurrencyCollection ResourceCost(ResourceElement machine, ResourceItemType resource = null)
        {
            Active(machine);
            if (resource != null && !machine.CanSupplyResource(resource))
                throw new ArgumentException("The machine cannot supply that resource.", nameof(resource));
            return machine.GetResourceCost(resource);
        }
        public float DesiredResourceOutput(ResourceElement machine)
        {
            Active(machine);
            return machine.GetDesiredOutput();
        }
        public float ResourceOutputPerSecond(ResourceElement machine)
        {
            Active(machine);
            return machine.OutputPerSecond;
        }
        public float RefinementFailureChance(Refiner machine, InventoryItem item, bool? useMana = null)
        {
            Active(machine);
            if (item == null) throw new ArgumentNullException(nameof(item));
            return machine.GetFailChance(item, useMana);
        }
        public int RefinementManaCost(Refiner machine, InventoryItem item)
        {
            Active(machine);
            if (item == null) throw new ArgumentNullException(nameof(item));
            return machine.GetManaCost(item);
        }
        public void CollapseCollector(Collector machine) { Active(machine); machine.Collapse(); }
        public void CollapseDistributor(Distributor machine) { Active(machine); machine.Collapse(); }
        public void CollapseManifold(Manifold machine) { Active(machine); machine.Collapse(); }

    }
}
