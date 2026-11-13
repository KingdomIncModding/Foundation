using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.SpendCurrency), new[] { typeof(Currency), typeof(long) })]
    internal static class SpendCurrency
    {
        static void Prefix(GamePlayer __instance, Currency c, out long __state) => __state = __instance.GetCurrency(c);
        static void Postfix(GamePlayer __instance, Currency c, bool __result, long __state)
        {
            if (!__result)
                return;
            var current = __instance.GetCurrency(c);
            if (current != __state)
                global::KingdomFoundation.GameEventIntegration.Publish(new CurrencyChangedEvent(__instance, c, __state, current));
        }
    }
}
