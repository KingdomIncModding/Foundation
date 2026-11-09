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
    internal static partial class GameplayIntegration
    {
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.DoAscension))]
        private static class Ascended
        {
            static void Postfix(GamePlayer __instance) => Publish(new KingdomAscendedEvent(__instance));
        }
    }

}
