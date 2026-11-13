using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal static class GameCurrencyMap
    {
        internal static Currency Resolve(string id)
        {
            if(!Enum.TryParse<Currency>(id,out var value) || !Enum.IsDefined(typeof(Currency),value)) throw new ArgumentException("Unknown currency identifier.",nameof(id));
            return value;
        }
    }
}
