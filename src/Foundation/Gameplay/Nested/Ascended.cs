using System;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.DoAscension))]
    internal static class Ascended
    {
        static void Postfix(GamePlayer __instance) => global::KingdomFoundation.GameplayIntegration.Publish(new KingdomAscendedEvent(__instance));
    }
}
