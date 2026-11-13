using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.CreateNewElement))]
    internal static class Created
    {
        static void Postfix(FlowElement el) => global::KingdomFoundation.WorldEventIntegration.Safely(() =>
        {
            var player = GamePlayer.Current;
            if (player != null && player.Flows.Count > 0)
                FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.Created, player.ActiveFlow, el, player.CurrentFlow));
        });
    }
}
