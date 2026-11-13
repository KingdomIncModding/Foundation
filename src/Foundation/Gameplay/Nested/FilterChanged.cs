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
    [HarmonyPatch(typeof(Filter), nameof(Filter.SetFilterType))]
    internal static class FilterChanged
    {
        static void Prefix(Filter __instance, out ItemType __state) => __state = __instance.FilterType;
        static void Postfix(Filter __instance, ItemType __state)
        {
            if (__state != __instance.FilterType)
                global::KingdomFoundation.GameplayIntegration.Publish(new MachineFilterChangedEvent(__instance, __state, __instance.FilterType));
        }
    }
}
