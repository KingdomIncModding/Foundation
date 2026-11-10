using Assets.Source.Player;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the world changed event API.</summary>
    public sealed class WorldChangedEvent
    {
        public GamePlayer Previous { get; }
        public GamePlayer Current { get; }
        public WorldChangedEvent(GamePlayer previous, GamePlayer current)
        { Previous = previous; Current = current; }
    }
}
