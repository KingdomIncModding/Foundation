using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class MachineDungeonChangedEvent
    {
        public Dungeon Machine { get; }
        public DungeonType Previous { get; }
        public DungeonType Current { get; }
        public MachineDungeonChangedEvent(Dungeon machine, DungeonType previous, DungeonType current)
        { Machine = machine; Previous = previous; Current = current; }
    }
}
