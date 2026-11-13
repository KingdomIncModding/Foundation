using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.UnlockWar))]
    internal static class WarUnlocked
    {
        static void Prefix(GamePlayer __instance, WarType warType, out bool __state) => __state = __instance.GetWarProgress(warType) != null;
        static void Postfix(GamePlayer __instance, WarType warType, bool __state)
        {
            if (!__state && __instance.GetWarProgress(warType) != null)
                global::KingdomFoundation.GameEventIntegration.Publish(new WarUnlockedEvent(__instance, warType));
        }
    }
}
