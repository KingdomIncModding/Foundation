using System;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>
    /// Isolates native element counts for <see cref="HostedWorkspaceNode"/> instances by definition ID (<see cref="FlowElement.ElementType"/>)
    /// while preserving standard native CLR-type counting for native and custom <see cref="WorkspaceNode"/> subclasses.
    /// </summary>
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.GetElementCount))]
    internal static class HostedNodeElementCount
    {
        static bool Prefix(GamePlayer __instance, FlowElement type, ref int __result)
        {
            if (!(type is HostedWorkspaceNode target))
                return true;
            int count = 0;
            string targetId = target.ElementType;
            foreach (var flow in __instance.Flows)
            {
                if (flow?.Elements == null) continue;
                foreach (var element in flow.Elements)
                {
                    if (element is HostedWorkspaceNode hosted && string.Equals(hosted.ElementType, targetId, StringComparison.Ordinal))
                        count += element.StackSize;
                }
            }
            __result = count;
            return false;
        }
    }
}

