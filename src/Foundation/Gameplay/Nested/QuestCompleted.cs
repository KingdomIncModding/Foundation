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
        [HarmonyPatch(typeof(QuestProgress), nameof(QuestProgress.QuestCompleted))]
        private static class QuestCompleted
        {
            static void Postfix(QuestProgress __instance, bool __result)
            { if (__result) Publish(new QuestCompletedEvent(__instance)); }
        }
    }

}
