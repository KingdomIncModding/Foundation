using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using HarmonyLib;

namespace KingdomFoundation.Machines
{
    /// <summary>Observe only completed native WorldFlowManager pipe placements, never ghost connections.</summary>
    [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.CreateNewPipe))]
    internal static class NativeIOPipeCreatedPatch
    {
        private static void Postfix(WorldFlowManager __instance, FlowPipe pipe)
        {
            try
            {
                if (__instance && pipe != null && __instance.CurrentFlow.Pipes.Contains(pipe) && pipe.ConnectedInput != null && pipe.ConnectedOutput != null)
                {
                    int flowIndex = MachineThread.Player?.Flows.IndexOf(__instance.CurrentFlow) ?? -1;
                    if (flowIndex >= 0)
                        FoundationMachineApi.IO.Publish(pipe, flowIndex, true);
                }
            }
            catch (Exception error)
            {
                MachineRuntime.ReportCallbackError("Native IO pipe committed", error);
            }
        }
    }
}
