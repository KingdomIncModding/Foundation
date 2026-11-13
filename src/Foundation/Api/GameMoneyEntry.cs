using UnityEngine;

namespace KingdomFoundation.Api
{
    public sealed class GameMoneyEntry
    {
        public string CurrencyId { get; }
        public long Amount { get; }
        public Sprite Icon { get; }
        internal GameMoneyEntry(string id, long amount, Sprite icon) { CurrencyId = id; Amount = amount; Icon = icon; }
    }
}
