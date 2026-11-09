using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>All values are read after the corresponding native operation has completed.</summary>
    public sealed class CurrencyChangedEvent
    {
        public GamePlayer Player { get; }
        public Currency Currency { get; }
        public long Previous { get; }
        public long Current { get; }
        public long Delta => Current - Previous;
        public CurrencyChangedEvent(GamePlayer player, Currency currency, long previous, long current)
        { Player = player; Currency = currency; Previous = previous; Current = current; }
    }
}
