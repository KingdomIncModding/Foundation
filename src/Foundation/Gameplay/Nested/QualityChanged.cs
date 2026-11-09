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
        [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.SetQuality))]
        private static class QualityChanged
        {
            static void Prefix(InventoryItem __instance, out float __state) => __state = __instance.Quality;
            static void Postfix(InventoryItem __instance, float __state)
            { if (__state != __instance.Quality) Publish(new ItemQualityChangedEvent(__instance, __state, __instance.Quality)); }
        }
    }

}
