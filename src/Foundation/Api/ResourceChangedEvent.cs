using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class ResourceChangedEvent
    {
        public GamePlayer Player { get; }
        public ResourceItemType Resource { get; }
        public double Previous { get; }
        public double Current { get; }
        public double Delta => Current - Previous;
        public ResourceChangedEvent(GamePlayer player, ResourceItemType resource, double previous, double current)
        { Player = player; Resource = resource; Previous = previous; Current = current; }
    }
}
