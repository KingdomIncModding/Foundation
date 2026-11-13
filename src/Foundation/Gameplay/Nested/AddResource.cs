using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddResource))]
    internal static class AddResource
    {
        static void Prefix(GamePlayer __instance, ResourceItemType resource, out double __state) => __state = __instance.GetResourceAvailable(resource);
        static void Postfix(GamePlayer __instance, ResourceItemType resource, double __state)
        {
            var current = __instance.GetResourceAvailable(resource);
            if (current != __state)
                global::KingdomFoundation.GameEventIntegration.Publish(new ResourceChangedEvent(__instance, resource, __state, current));
        }
    }
}
