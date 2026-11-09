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
        [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.CreateNewElement))]
        private static class Created
        {
            static void Postfix(FlowElement el) => Safely(() =>
            {
                var player = GamePlayer.Current;
                if (player != null && player.Flows.Count > 0)
                    FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.Created,
                        player.ActiveFlow, el, player.CurrentFlow));
            });
        }
    }

}
