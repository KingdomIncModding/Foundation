using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class PrestigeChangedEvent
    {
        public GamePlayer Player { get; }
        public PrestigeNode Node { get; }
        public int Previous { get; }
        public int Current { get; }
        public PrestigeChangedEvent(GamePlayer player, PrestigeNode node, int previous, int current)
        { Player = player; Node = node; Previous = previous; Current = current; }
    }
}
