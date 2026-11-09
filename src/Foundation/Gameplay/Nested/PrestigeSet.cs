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
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.SetPrestigePoints))]
        private static class PrestigeSet
        {
            static void Prefix(GamePlayer __instance, PrestigeNode node, out int __state) => __state = __instance.GetPrestigePoints(node);
            static void Postfix(GamePlayer __instance, PrestigeNode node, int __state)
            {
                var current = __instance.GetPrestigePoints(node);
                if (current != __state) Publish(new PrestigeChangedEvent(__instance, node, __state, current));
            }
        }
    }

}
