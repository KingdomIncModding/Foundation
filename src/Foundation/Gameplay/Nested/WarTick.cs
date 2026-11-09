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
        [HarmonyPatch(typeof(WarProgress), nameof(WarProgress.UpdateWar))]
        private static class WarTick
        {
            static void Prefix(WarProgress __instance, out int __state) => __state = __instance.CurrentStage;
            static void Postfix(WarProgress __instance, int __state)
            { if (__state != __instance.CurrentStage) Publish(new WarStageChangedEvent(__instance, __state, __instance.CurrentStage)); }
        }
    }

}
