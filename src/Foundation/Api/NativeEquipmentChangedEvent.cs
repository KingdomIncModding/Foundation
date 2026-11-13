using Assets.Source.Items;
using Assets.Source.Items.Data;

namespace KingdomFoundation.Api
{
    /// <summary>Observed after the game's own AddEquipment or RemoveEquipment succeeds.</summary>
    public sealed class NativeEquipmentChangedEvent
    {
        public HumanItemData Human { get; }
        public InventoryItem Item { get; }
        public bool Equipped { get; }
        public NativeEquipmentChangedEvent(HumanItemData human, InventoryItem item, bool equipped)
        { Human = human; Item = item; Equipped = equipped; }
    }
}
