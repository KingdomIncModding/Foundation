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
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddTech))]
        private static class UnlockTech
        {
            static void Prefix(GamePlayer __instance, TechNode tech, out bool __state)
                => __state = tech != null && __instance.HasTech(tech);
            static void Postfix(GamePlayer __instance, TechNode tech, bool __state)
            {
                if (!__state && tech != null && __instance.HasTech(tech)) Publish(new ResearchUnlockedEvent(__instance, tech));
            }
        }
    }

}
