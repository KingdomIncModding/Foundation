using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class FlowIntegration
    {
        [HarmonyPatch(typeof(FlowPipe), nameof(FlowPipe.Connect))]
        private static class PipeConnect
        {
            private static void Postfix(FlowPipe __instance, bool ghost)
            {
                // Native ghost connections do not modify either port's ConnectedPipe.
                if (!ghost && __instance.ConnectedOutput != null && __instance.ConnectedInput != null)
                    Safely(() => FoundationApi.FlowEvents.RaiseConnected(__instance));
            }
        }
    }

}
