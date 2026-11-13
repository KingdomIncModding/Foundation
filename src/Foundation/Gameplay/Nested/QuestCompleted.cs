using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(QuestProgress), nameof(QuestProgress.QuestCompleted))]
    internal static class QuestCompleted
    {
        static void Postfix(QuestProgress __instance, bool __result)
        {
            if (__result)
                global::KingdomFoundation.GameEventIntegration.Publish(new QuestCompletedEvent(__instance));
        }
    }
}
