using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Primitive live production values. Consumers decide what these values mean for their feature.</summary>
    public sealed class GameProductionState
    {
        private readonly FlowElement element;
        internal GameProductionState(FlowElement element) { this.element=element; }
        public float Progress=>element.Progress;
        public int InputCount=>element.Inputs?.Count ?? 0;
        public int OutputCount=>element.Outputs?.Count ?? 0;
        public int BufferedInputCount(int index)=>element.Inputs[index].BufferedItems.Count;
        public bool OutputCanBuffer(int index)=>element.Outputs[index].CanBuffer;
        public bool IsStockpile=>element is Stockpile;
        public bool HasStorageContents=>(element as Stockpile)?.Contents!=null;
        public int StoredItemCount=>(element as Stockpile)?.Contents?.Count ?? 0;
        public int StorageCapacity=>(element as Stockpile)?.Capacity ?? 0;
        public bool IsArmory=>element is Armory;
        public bool HasArmoryType=>(element as Armory)?.ArmoryType!=null;
        public bool CanStoreArmoryItem=>element is Armory armory && GamePlayer.Current!=null && GamePlayer.Current.Armory.CanAddTo(armory.Channel,armory.ArmoryType,armory.MaxItems);
        public bool IsShop=>element is Shop;
        public long ShopGoldCap=>(element as Shop)?.GoldCap ?? 0;
        public long CollectedGold=>(element as Shop)?.CollectedGold ?? 0;
        public bool IsRecipe=>element is RecipeElement;
        public GameCraftingInput Input=>GameCraftingInput.Wrap((element as RecipeElement)?.Input);
        public GameInventoryItem CurrentHuman=>GameInventoryItem.Wrap((element as Outfitter)?.CurrentHuman);
        public GameInventoryItem CurrentEquipment=>GameInventoryItem.Wrap((element as Outfitter)?.CurrentEquip);
        public GameItemType Resource=>GameItemType.Wrap((element as ResourceElement)?.Resource);
        public GameItemType SelectedForestResource=>GameItemType.Wrap((element as Forest)?.SelectedResource);
        public int QueuedCount=>element is ResourceElement source ? source.QueuedCount : element is Forest forest ? forest.QueuedCount : 0;
        public int StoredValue=>(element as RequisitionOffice)?.StoredValue ?? 0;
        public long MaximumGold=>(element as Shop)?.MaxGold ?? 0;
        public int MaximumRequisition=>(element as RequisitionOffice)?.MaxRequisition ?? 0;
        public float PassiveTimer=>element is RequisitionOffice office ? OfficeTimer(office) : element is Disenchanter disenchanter ? ManaTimer(disenchanter) : 0f;
        public float PassiveGain=>element is RequisitionOffice office ? OfficeGain(office) : element is Disenchanter disenchanter ? ManaGain(disenchanter) : 0f;
        public int ValuePerRequisition=>element is RequisitionOffice office ? OfficeValue(office) : 0;
        private static readonly HarmonyLib.AccessTools.FieldRef<RequisitionOffice,float> OfficeTimer=HarmonyLib.AccessTools.FieldRefAccess<RequisitionOffice,float>("_passiveTimer");
        private static readonly HarmonyLib.AccessTools.FieldRef<RequisitionOffice,float> OfficeGain=HarmonyLib.AccessTools.FieldRefAccess<RequisitionOffice,float>("_passiveGain");
        private static readonly HarmonyLib.AccessTools.FieldRef<RequisitionOffice,int> OfficeValue=HarmonyLib.AccessTools.FieldRefAccess<RequisitionOffice,int>("_valuePerReq");
        private static readonly HarmonyLib.AccessTools.FieldRef<Disenchanter,float> ManaTimer=HarmonyLib.AccessTools.FieldRefAccess<Disenchanter,float>("_passiveTimer");
        private static readonly HarmonyLib.AccessTools.FieldRef<Disenchanter,int> ManaGain=HarmonyLib.AccessTools.FieldRefAccess<Disenchanter,int>("_passiveGain");
        public GameMoney ResourceCost {get {var cost=element is ResourceElement source ? source.GetResourceCost() : element is Forest forest ? forest.GetResourceCost() : null;return cost==null ? null : new GameMoney(cost);}}
        public string DefaultLabel=>element is ResourceElement resource ? resource.DefaultLabel : element is RecipeElement recipe ? recipe.DefaultLabel : element.Label;
        public string RecipeDisplayName=>(element as RecipeElement)?.Recipe?.DisplayName;
        public bool HasEnhancementResult=>(element as RecipeElement)?.Recipe?.Results?.Any(result=>result.Enhancement!=null) ?? false;
        public GameWar War=>element is WarEffort effort ? GameWar.Wrap(effort.WarType) : null;
        public long CollectedCurrency(GameCurrency currency)
        {
            switch(currency)
            {
                case GameCurrency.Gold: return element is Shop shop ? shop.CollectedGold : element is Restaurant restaurant ? restaurant.CollectedGold : 0;
                case GameCurrency.Mana: return (element as Disenchanter)?.CollectedMana ?? 0;
                case GameCurrency.Crown: return (element as Restaurant)?.CollectedCrowns ?? 0;
                case GameCurrency.Requisition: return (element as RequisitionOffice)?.CollectedRequisition ?? 0;
                default:return 0;
            }
        }
        /// <summary>Raw native visitors, including non-human arena occupants. Consumers choose how to classify them.</summary>
        public IEnumerable<GameInventoryItem> Visitors
        {
            get
            {
                if(element is TrainingGrounds training)
                    foreach(var slot in training.Contents){if(slot?.Human!=null)yield return GameInventoryItem.Wrap(slot.Human);}
                else if(element is Arena arena)
                {if(arena.Slot1?.Item!=null)yield return GameInventoryItem.Wrap(arena.Slot1.Item);if(arena.Slot2?.Item!=null)yield return GameInventoryItem.Wrap(arena.Slot2.Item);}
                else if(element is QuestBoard quest)
                    foreach(var visitor in quest.Contents){if(visitor?.Item!=null)yield return GameInventoryItem.Wrap(visitor.Item);}
            }
        }
        public bool HasRecipe=>(element as RecipeElement)?.Recipe!=null;
        public bool Crafting=>(element as RecipeElement)?.Crafting ?? false;
        public bool HasCraftingInput=>(element as RecipeElement)?.Input!=null;
        public int ReagentCount=>(element as RecipeElement)?.Recipe?.Reagents?.Length ?? 0;
        public int ReagentAmount(int index)=>((RecipeElement)element).Recipe.Reagents[index].Count;
        public GameMoney CraftingCost {get {var cost=(element as RecipeElement)?.GetCraftingCost();return cost==null ? null : new GameMoney(cost);}}
        public bool IsResource=>element is ResourceElement;
        public bool HasResource=>(element as ResourceElement)?.Resource!=null;
        public int QueuedResourceCount=>(element as ResourceElement)?.QueuedCount ?? 0;
        public float ResourceQuotaMultiplier=>element is ResourceElement source && source.Resource!=null && GamePlayer.Current!=null ? GamePlayer.Current.GetResourceQuotaMultiplier(source.Resource) : 1f;
    }
}
