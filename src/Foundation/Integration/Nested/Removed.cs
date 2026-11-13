using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.RemoveElement))]
    internal static class Removed
    {
        static void Prefix(FlowElement element, out WorkspaceChangedEvent __state)
        {
            WorkspaceChangedEvent captured = null;
            global::KingdomFoundation.WorldEventIntegration.Safely(() =>
            {
                var player = GamePlayer.Current;
                if (player != null && player.Flows.Count > 0)
                    captured = new WorkspaceChangedEvent(WorkspaceChangeKind.Removed, player.ActiveFlow, element, player.CurrentFlow);
            });
            __state = captured;
        }

        static void Postfix(WorkspaceChangedEvent __state)
        {
            if (__state != null)
                global::KingdomFoundation.WorldEventIntegration.Safely(() => FoundationApi.Events.Publish(__state));
        }
    }
}
