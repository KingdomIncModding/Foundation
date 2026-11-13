using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowElement), nameof(WorldFlowElement.UpdatePosition))]
    internal static class Moved
    {
        static void Prefix(WorldFlowElement __instance, out UnityEngine.Vector2Int __state) => __state = __instance.Element != null ? __instance.Element.Position : UnityEngine.Vector2Int.zero;
        static void Postfix(WorldFlowElement __instance, bool __result, UnityEngine.Vector2Int __state)
        {
            if (!__result || __instance.Element == null || __instance.Element.Position == __state)
                return;
            global::KingdomFoundation.WorldEventIntegration.Safely(() =>
            {
                var player = GamePlayer.Current;
                if (player != null && player.Flows.Count > 0)
                    FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.Moved, __instance.Element.ParentFlow, __instance.Element, player.CurrentFlow));
            });
        }
    }
}
