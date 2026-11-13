using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Items.Data;
using HarmonyLib;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class GameItemsService : IGameItemsService
    {
        public IReadOnlyList<ItemType> Items => Array.AsReadOnly(ItemType.All.ToArray());
        public IReadOnlyList<CraftingRecipe> Recipes => Array.AsReadOnly(CraftingRecipe.All.ToArray());
        // ItemEnhancement has no public All catalog in the game's assembly.
        // A read-only view of the exact game-owned registry is the A3 bridge;
        // Foundation does not create a second enhancement registry.
        private static IReadOnlyDictionary<string, ItemEnhancement> EnhancementCatalog
        {
            get
            {
                // C# 14 reserves `field` inside property accessors for the
                // compiler-generated backing field; never use it as a local name.
                var enhancementRegistryField = AccessTools.Field(typeof(ItemEnhancement), "_allEnhancements");
                if (enhancementRegistryField == null)
                    throw new MissingFieldException(typeof(ItemEnhancement).FullName, "_allEnhancements");
                return (IReadOnlyDictionary<string, ItemEnhancement>)enhancementRegistryField.GetValue(null);
            }
        }
        public IReadOnlyList<ItemEnhancement> Enhancements
            => Array.AsReadOnly(EnhancementCatalog.Values.OrderBy(e => e.Identifier, StringComparer.Ordinal).ToArray());
        public ItemEnhancement FindEnhancement(string id)
            => id != null && EnhancementCatalog.TryGetValue(id, out var value) ? value : null;
        public IReadOnlyList<ItemEnhancementData> EquipmentEnhancements(InventoryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return item.Data is EquipmentItemData equip
                ? Array.AsReadOnly(equip.Enhancements.ToArray())
                : Array.Empty<ItemEnhancementData>();
        }
        public int EquipmentStat(InventoryItem item, EquipStat stat)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return item.Data is EquipmentItemData equip ? equip.GetStat(stat) : 0;
        }
        public bool IsUnlocked(ItemType type) => (type ?? throw new ArgumentNullException(nameof(type))).IsUnlocked;
        public long NativeBaseValue(ItemType type) => (type ?? throw new ArgumentNullException(nameof(type))).BaseValue;
        public long NativeSellValue(InventoryItem item) => (item ?? throw new ArgumentNullException(nameof(item))).SellValue;
        public int AppraiserStat(InventoryItem item, AppraiserStat stat)
            => (item ?? throw new ArgumentNullException(nameof(item))).GetAppraiserStat(stat);
        public bool CanEnhance(InventoryItem item, EnhancementType type)
            => (item ?? throw new ArgumentNullException(nameof(item))).CanEnhance(type);
        public bool RecipeAvailable(CraftingRecipe recipe)
            => (recipe ?? throw new ArgumentNullException(nameof(recipe))).IsAvailable;
        public CurrencyCollection RecipeCost(CraftingRecipe recipe)
            => (recipe ?? throw new ArgumentNullException(nameof(recipe))).CraftingCost;
        public CurrencyCollection RecipeReward(CraftingRecipe recipe)
            => (recipe ?? throw new ArgumentNullException(nameof(recipe))).CraftingReward;
        public IReadOnlyList<CraftingReagent> RecipeReagents(CraftingRecipe recipe)
            => Array.AsReadOnly(((recipe ?? throw new ArgumentNullException(nameof(recipe))).Reagents ?? Array.Empty<CraftingReagent>()).ToArray());
        public IReadOnlyList<CraftingResult> RecipeResults(CraftingRecipe recipe)
            => Array.AsReadOnly(((recipe ?? throw new ArgumentNullException(nameof(recipe))).Results ?? Array.Empty<CraftingResult>()).ToArray());
        public int RequiredQuantity(CraftingRecipe recipe, ItemType item)
            => (recipe ?? throw new ArgumentNullException(nameof(recipe))).GetReagentCount(item ?? throw new ArgumentNullException(nameof(item)));
        public int ProducedQuantity(CraftingRecipe recipe, ItemType item)
            => (recipe ?? throw new ArgumentNullException(nameof(recipe))).GetResultCount(item ?? throw new ArgumentNullException(nameof(item)));
        public IReadOnlyList<string> NativeItemSources(ItemType item)
            => Array.AsReadOnly((item ?? throw new ArgumentNullException(nameof(item))).GetItemSources().ToArray());

        public ItemType FindItem(string id) => ItemType.All.FirstOrDefault(i => i.Identifier == id);
        public CraftingRecipe FindRecipe(string id) => CraftingRecipe.All.FirstOrDefault(r => r.Identifier == id);
        public InventoryItem CreateItem(string itemId, float quality = 1f, bool initialize = true)
        {
            var type = FindItem(itemId) ?? throw new ArgumentException("Unknown item: " + itemId, nameof(itemId));
            if (float.IsNaN(quality) || float.IsInfinity(quality) || quality <= 0) throw new ArgumentOutOfRangeException(nameof(quality));
            var item = new InventoryItem(type);
            item.SetQuality(quality);
            if (initialize) item.InitializeData();
            return item;
        }
        public InventoryItem Clone(InventoryItem original) => (original ?? throw new ArgumentNullException(nameof(original))).CloneItem();
        public InventoryItem Deserialize(JsonValue data) => InventoryItem.FromJson(data);
        public JsonValue Serialize(InventoryItem item) => (item ?? throw new ArgumentNullException(nameof(item))).ToJson();
        public IReadOnlyList<CraftingRecipe> Producing(ItemType type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Array.AsReadOnly(CraftingRecipe.All.Where(r => r.Results != null && r.GetResultCount(type) > 0).ToArray());
        }
        public IReadOnlyList<CraftingRecipe> Requiring(ItemType type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Array.AsReadOnly(CraftingRecipe.All.Where(r => r.Reagents != null && r.GetReagentCount(type) > 0).ToArray());
        }
        public bool CanAccept(Assets.Source.Flow.FlowInput input, InventoryItem item)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (item == null) throw new ArgumentNullException(nameof(item));
            return input.CanBuffer && input.CanAcceptItem(item);
        }
        public bool TryInsert(Assets.Source.Flow.FlowInput input, InventoryItem item)
        {
            if (!CanAccept(input, item)) return false;
            // This mutates a native IO buffer; callers own the item instance, not a clone.
            return input.InputItem(item);
        }
        public InventoryItem Extract(Assets.Source.Flow.FlowIO port)
            => (port ?? throw new ArgumentNullException(nameof(port))).ConsumeItem();
    }
}
