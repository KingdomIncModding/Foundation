using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class ItemQualityChangedEvent
    {
        public InventoryItem Item { get; }
        public float Previous { get; }
        public float Current { get; }
        public ItemQualityChangedEvent(InventoryItem item, float previous, float current)
        { Item = item; Previous = previous; Current = current; }
    }
}
