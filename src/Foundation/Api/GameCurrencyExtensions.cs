using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public static class GameCurrencyExtensions
    {
        public static Sprite GetIcon(this GameCurrency currency)=>GameCurrencyMap.Resolve(currency.ToString()).GetIcon();
        public static bool IsUnlocked(this GameCurrency currency)=>GameCurrencyMap.Resolve(currency.ToString()).IsUnlocked();
        public static string GetDisplayName(this GameCurrency currency)=>GameCurrencyMap.Resolve(currency.ToString()).GetDisplayName();
    }
}
