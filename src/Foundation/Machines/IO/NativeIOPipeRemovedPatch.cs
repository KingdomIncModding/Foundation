using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using HarmonyLib;

namespace KingdomFoundation.Machines
{
    /// <summary>Observe the actual native FlowDiagram deletion after it succeeds.</summary>
    [HarmonyPatch(typeof(FlowDiagram), nameof(FlowDiagram.DisconnectPipe))]
    internal static class NativeIOPipeRemovedPatch
    {
        private sealed class State
        {
            internal FlowPipe Pipe;
            internal int Flow;
        }

        private static void Prefix(FlowDiagram __instance, FlowPipe pipe, out State __state)
        {
            __state = null;
            try
            {
                if (pipe == null || __instance == null || !__instance.Pipes.Contains(pipe))
                    return;
                var game = MachineThread.Player;
                int flow = game?.Flows.IndexOf(__instance) ?? -1;
                if (flow >= 0)
                    __state = new State
                    {
                        Pipe = pipe,
                        Flow = flow
                    };
            }
            catch (Exception error)
            {
                MachineRuntime.ReportCallbackError("Native IO disconnect prestate", error);
            }
        }

        private static void Postfix(FlowDiagram __instance, State __state)
        {
            try
            {
                if (__state != null && !__instance.Pipes.Contains(__state.Pipe))
                    FoundationMachineApi.IO.Publish(__state.Pipe, __state.Flow, false);
            }
            catch (Exception error)
            {
                MachineRuntime.ReportCallbackError("Native IO pipe removed", error);
            }
        }
    }
}
