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
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.UnlockDungeon))]
        private static class DungeonUnlocked
        {
            static void Prefix(GamePlayer __instance, DungeonType dt, out bool __state) => __state = __instance.UnlockedDungeons.Contains(dt.Identifier);
            static void Postfix(GamePlayer __instance, DungeonType dt, bool __state)
            { if (!__state && __instance.UnlockedDungeons.Contains(dt.Identifier)) Publish(new DungeonUnlockedEvent(__instance, dt)); }
        }
    }

}
