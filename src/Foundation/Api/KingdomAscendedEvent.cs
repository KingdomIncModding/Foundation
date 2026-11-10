using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the kingdom ascended event API.</summary>
    public sealed class KingdomAscendedEvent
    {
        public GamePlayer Player { get; }
        public int AscensionCount { get; }
        public KingdomAscendedEvent(GamePlayer player) { Player = player; AscensionCount = player.AscensionCount; }
    }
}
