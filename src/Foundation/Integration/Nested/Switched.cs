using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class WorldEventIntegration
    {
        [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.ShowFlow))]
        private static class Switched
        {
            static void Postfix(int idx) => Safely(() =>
            {
                var player = GamePlayer.Current;
                if (player != null && idx >= 0 && idx < player.Flows.Count)
                    FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.FlowSwitched,
                        player.ActiveFlow, null, idx));
            });
        }
    }

}
