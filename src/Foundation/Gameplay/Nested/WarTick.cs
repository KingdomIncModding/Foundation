using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WarProgress), nameof(WarProgress.UpdateWar))]
    internal static class WarTick
    {
        static void Prefix(WarProgress __instance, out int __state) => __state = __instance.CurrentStage;
        static void Postfix(WarProgress __instance, int __state)
        {
            if (__state != __instance.CurrentStage)
                global::KingdomFoundation.GameEventIntegration.Publish(new WarStageChangedEvent(__instance, __state, __instance.CurrentStage));
        }
    }
}
