using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddTech))]
    internal static class UnlockTech
    {
        static void Prefix(GamePlayer __instance, TechNode tech, out bool __state) => __state = tech != null && __instance.HasTech(tech);
        static void Postfix(GamePlayer __instance, TechNode tech, bool __state)
        {
            if (!__state && tech != null && __instance.HasTech(tech))
                global::KingdomFoundation.GameEventIntegration.Publish(new ResearchUnlockedEvent(__instance, tech));
        }
    }
}
