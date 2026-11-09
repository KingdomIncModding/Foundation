using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class MachineFilterChangedEvent
    {
        public Filter Machine { get; }
        public ItemType Previous { get; }
        public ItemType Current { get; }
        public MachineFilterChangedEvent(Filter machine, ItemType previous, ItemType current)
        { Machine = machine; Previous = previous; Current = current; }
    }
}
