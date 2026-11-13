using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>Native item/recipe catalogs and serializable, quality-bearing item instances.</summary>
    public interface IGameItemsService
    {
        IReadOnlyList<ItemType> Items { get; }
        IReadOnlyList<CraftingRecipe> Recipes { get; }
        /// <summary>Native item unlock state (not a Foundation content registration).</summary>
        IReadOnlyList<ItemEnhancement> Enhancements { get; }
        ItemEnhancement FindEnhancement(string id);
        IReadOnlyList<ItemEnhancementData> EquipmentEnhancements(InventoryItem item);
        int EquipmentStat(InventoryItem item, EquipStat stat);
        bool IsUnlocked(ItemType type);
        long NativeBaseValue(ItemType type);
        long NativeSellValue(InventoryItem item);
        int AppraiserStat(InventoryItem item, AppraiserStat stat);
        bool CanEnhance(InventoryItem item, EnhancementType type);
        /// <summary>Native recipe gating, costs, rewards, requirements and outputs.</summary>
        bool RecipeAvailable(CraftingRecipe recipe);
        CurrencyCollection RecipeCost(CraftingRecipe recipe);
        CurrencyCollection RecipeReward(CraftingRecipe recipe);
        IReadOnlyList<CraftingReagent> RecipeReagents(CraftingRecipe recipe);
        IReadOnlyList<CraftingResult> RecipeResults(CraftingRecipe recipe);
        int RequiredQuantity(CraftingRecipe recipe, ItemType item);
        int ProducedQuantity(CraftingRecipe recipe, ItemType item);
        IReadOnlyList<string> NativeItemSources(ItemType item);

        ItemType FindItem(string id);
        CraftingRecipe FindRecipe(string id);
        InventoryItem CreateItem(string itemId, float quality = 1f, bool initialize = true);
        InventoryItem Clone(InventoryItem original);
        InventoryItem Deserialize(JsonValue json);
        JsonValue Serialize(InventoryItem item);
        IReadOnlyList<CraftingRecipe> Producing(ItemType type);
        IReadOnlyList<CraftingRecipe> Requiring(ItemType type);
        bool CanAccept(Assets.Source.Flow.FlowInput input, InventoryItem item);
        bool TryInsert(Assets.Source.Flow.FlowInput input, InventoryItem item);
        InventoryItem Extract(Assets.Source.Flow.FlowIO port);
    }
}
