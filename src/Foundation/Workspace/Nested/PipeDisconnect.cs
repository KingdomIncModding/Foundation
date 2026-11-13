using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowDiagram), nameof(FlowDiagram.DisconnectPipe))]
    internal static class PipeDisconnect
    {
        private static void Prefix(FlowDiagram __instance, FlowPipe pipe, out FlowPipeSnapshot __state)
        {
            // Capture endpoints before vanilla clears their ConnectedPipe field.
            // Only report removals of pipes actually registered in the diagram.
            FlowPipeSnapshot snapshot = null;
            global::KingdomFoundation.FlowIntegration.Safely(() =>
            {
                if (pipe != null && __instance.Pipes.Contains(pipe))
                    snapshot = new FlowPipeSnapshot(pipe);
            });
            __state = snapshot;
        }

        private static void Postfix(FlowPipeSnapshot __state)
        {
            if (__state != null)
                global::KingdomFoundation.FlowIntegration.Safely(() => FoundationApi.FlowEvents.RaiseDisconnected(__state));
        }
    }
}
