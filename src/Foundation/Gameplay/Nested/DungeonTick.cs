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
        [HarmonyPatch(typeof(DungeonProgress), nameof(DungeonProgress.DungeonTick))]
        private static class DungeonTick
        {
            static void Prefix(DungeonProgress __instance, out int __state) => __state = __instance.CurrentStage;
            static void Postfix(DungeonProgress __instance, int __state)
            { if (__state != __instance.CurrentStage) Publish(new DungeonStageChangedEvent(__instance, __state, __instance.CurrentStage)); }
        }
    }

}
