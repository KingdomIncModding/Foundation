using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class WarUnlockedEvent
    {
        public GamePlayer Player { get; }
        public WarType War { get; }
        public WarUnlockedEvent(GamePlayer player, WarType war) { Player = player; War = war; }
    }
}
