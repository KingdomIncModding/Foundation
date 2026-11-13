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
    [HarmonyPatch(typeof(QuestBoard), nameof(QuestBoard.UpdateQuest))]
    internal static class QuestChanged
    {
        static void Prefix(QuestBoard __instance, out Quest __state) => __state = __instance.SelectedQuest?.Quest;
        static void Postfix(QuestBoard __instance, Quest __state)
        {
            var now = __instance.SelectedQuest?.Quest;
            if (__state != now)
                global::KingdomFoundation.GameplayIntegration.Publish(new MachineQuestChangedEvent(__instance, __state, now));
        }
    }
}
