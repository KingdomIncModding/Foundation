using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowPipe), nameof(WorldFlowPipe.ItemTransferred))]
    internal static class ItemTransferred
    {
        private static void Postfix(WorldFlowPipe __instance, InventoryItem item)
        {
            // Native FlowPipe.Update only calls this after the destination accepts
            // the item and the source consumes it. It is not a rate limit.
            if (__instance.Pipe != null && item != null)
            {
                global::KingdomFoundation.FlowIntegration.Safely(() =>
                {
                    FoundationApi.FlowOperationsInternal.Record(__instance.Pipe);
                    FoundationApi.FlowEvents.RaiseItemTransferred(__instance.Pipe, item);
                });
            }
        }
    }
}
