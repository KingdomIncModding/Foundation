using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class GameEventIntegration
    {
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddCurrency), new[] { typeof(Currency), typeof(long), typeof(bool) })]
        private static class AddCurrency
        {
            static void Prefix(GamePlayer __instance, Currency c, out long __state) => __state = __instance.GetCurrency(c);
            static void Postfix(GamePlayer __instance, Currency c, long __state)
            {
                var current = __instance.GetCurrency(c);
                if (current != __state) Publish(new CurrencyChangedEvent(__instance, c, __state, current));
            }
        }
    }

}
