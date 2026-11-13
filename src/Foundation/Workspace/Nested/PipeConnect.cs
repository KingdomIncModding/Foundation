using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowPipe), nameof(FlowPipe.Connect))]
    internal static class PipeConnect
    {
        private static void Postfix(FlowPipe __instance, bool ghost)
        {
            // Native ghost connections do not modify either port's ConnectedPipe.
            if (!ghost && __instance.ConnectedOutput != null && __instance.ConnectedInput != null)
                global::KingdomFoundation.FlowIntegration.Safely(() => FoundationApi.FlowEvents.RaiseConnected(__instance));
        }
    }
}
