using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.SetFarmResource))]
    internal static class SetFarmResource
    {
        static void Prefix(GamePlayer __instance, FarmItemType item, out double __state) => __state = __instance.GetResourceAvailable(item);
        static void Postfix(GamePlayer __instance, FarmItemType item, double __state)
        {
            var current = __instance.GetResourceAvailable(item);
            if (current != __state)
                global::KingdomFoundation.GameEventIntegration.Publish(new ResourceChangedEvent(__instance, item, __state, current));
        }
    }
}
