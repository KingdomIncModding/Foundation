using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the research unlocked event API.</summary>
    public sealed class ResearchUnlockedEvent
    {
        public GamePlayer Player { get; }
        public TechNode Node { get; }
        public ResearchUnlockedEvent(GamePlayer player, TechNode node) { Player = player; Node = node; }
    }
}
