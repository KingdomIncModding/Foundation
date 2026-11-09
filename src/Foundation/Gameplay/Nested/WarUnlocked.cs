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
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.UnlockWar))]
        private static class WarUnlocked
        {
            static void Prefix(GamePlayer __instance, WarType warType, out bool __state) => __state = __instance.GetWarProgress(warType) != null;
            static void Postfix(GamePlayer __instance, WarType warType, bool __state)
            { if (!__state && __instance.GetWarProgress(warType) != null) Publish(new WarUnlockedEvent(__instance, warType)); }
        }
    }

}
