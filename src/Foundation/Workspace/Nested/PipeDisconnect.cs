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
        [HarmonyPatch(typeof(FlowDiagram), nameof(FlowDiagram.DisconnectPipe))]
        private static class PipeDisconnect
        {
            private static void Prefix(FlowDiagram __instance, FlowPipe pipe, out FlowPipeSnapshot __state)
            {
                // Capture endpoints before vanilla clears their ConnectedPipe field.
                // Only report removals of pipes actually registered in the diagram.
                FlowPipeSnapshot snapshot = null;
                Safely(() =>
                {
                    if (pipe != null && __instance.Pipes.Contains(pipe)) snapshot = new FlowPipeSnapshot(pipe);
                });
                __state = snapshot;
            }

            private static void Postfix(FlowPipeSnapshot __state)
            {
                if (__state != null) Safely(() => FoundationApi.FlowEvents.RaiseDisconnected(__state));
            }
        }
    }

}
