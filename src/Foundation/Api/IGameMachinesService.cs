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
    /// <summary>
    /// Game machine actions routed to the real FlowElement subclasses. All mutators require
    /// placement in a live kingdom workspace and must be called on Unity's main thread.
    /// </summary>
    public interface IGameMachinesService
    {
        IReadOnlyList<FlowElement> Catalog { get; }
        IReadOnlyList<FlowElement> BuildableCatalog { get; }
        IReadOnlyList<T> Find<T>() where T : FlowElement;
        /// <summary>Invokes the configure recipe operation.</summary>
        /// <param name="machine">The machine argument.</param>
        /// <param name="recipe">The recipe argument.</param>
        /// <returns>The operation result.</returns>
        bool ConfigureRecipe(RecipeElement machine, CraftingRecipe recipe);
        bool ConfigureForest(Forest machine, ForestItemType resource);
        void SetFilter(Filter machine, ItemType item);
        /// <summary>Sets switch.</summary>
        void SetSwitch(TrackSwitch machine, bool enabled);
        void SetTimer(Timer machine, float seconds);
        void SetAppraiser(Appraiser machine, AppraiserStat? stat, AppraiserType op, int threshold);
        void AddDistributorOutput(Distributor machine);
        /// <summary>Invokes the add manifold input operation.</summary>
        /// <param name="machine">The machine argument.</param>
        void AddManifoldInput(Manifold machine);
        void AddCollectorInput(Collector machine);
        void AdvanceCollector(Collector machine);
        void SetArmoryOutput(Armory machine, ItemType type);
        /// <summary>Sets portal output.</summary>
        void SetPortalOutput(Portal machine, bool output);
        void SetPortalChannel(Portal machine, int channel, int index);
        void SetArmoryChannel(Armory machine, int channel);
        void SelectQuest(QuestBoard machine, Quest quest);
        /// <summary>Invokes the select dungeon operation.</summary>
        /// <param name="machine">The machine argument.</param>
        /// <param name="dungeon">The dungeon argument.</param>
        void SelectDungeon(Dungeon machine, DungeonType dungeon);
        void SelectRequisition(RequisitionOffice machine, int index);
        void CollectShop(Shop machine);
        void CollectRequisition(RequisitionOffice machine);

        // Native capability queries: the game's recipe/resource rules remain authoritative.
        /// <summary>Performs the available recipes operation.</summary>
        IReadOnlyList<CraftingRecipe> AvailableRecipes(RecipeElement machine);
        bool CanCraft(RecipeElement machine, CraftingRecipe recipe);
        Assets.Source.Player.CurrencyCollection CraftingCost(RecipeElement machine, CraftingRecipe recipe = null);
        IReadOnlyList<ResourceItemType> AvailableResources(ResourceElement machine);
        /// <summary>Invokes the resource cost operation.</summary>
        /// <param name="machine">The machine argument.</param>
        /// <param name="resource">The resource argument.</param>
        /// <returns>The operation result.</returns>
        Assets.Source.Player.CurrencyCollection ResourceCost(ResourceElement machine, ResourceItemType resource = null);
        float DesiredResourceOutput(ResourceElement machine);
        float ResourceOutputPerSecond(ResourceElement machine);
        float RefinementFailureChance(Refiner machine, InventoryItem item, bool? useMana = null);
        /// <summary>Performs the refinement mana cost operation.</summary>
        int RefinementManaCost(Refiner machine, InventoryItem item);
        void CollapseCollector(Collector machine);
        void CollapseDistributor(Distributor machine);
        void CollapseManifold(Manifold machine);
    }
}
