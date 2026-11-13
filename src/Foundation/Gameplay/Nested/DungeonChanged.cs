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
    [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.SetDungeonType))]
    internal static class DungeonChanged
    {
        static void Prefix(Dungeon __instance, out DungeonType __state) => __state = __instance.CurrentDungeon?.Dungeon;
        static void Postfix(Dungeon __instance, DungeonType __state)
        {
            var now = __instance.CurrentDungeon?.Dungeon;
            if (__state != now)
                global::KingdomFoundation.GameplayIntegration.Publish(new MachineDungeonChangedEvent(__instance, __state, now));
        }
    }
}
