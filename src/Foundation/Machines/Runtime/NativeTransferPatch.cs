using System;
using Assets.Behaviour.Flow;
using Assets.Source.Items;
using HarmonyLib;

namespace KingdomFoundation.Machines
{
    [HarmonyPatch(typeof(WorldFlowPipe), nameof(WorldFlowPipe.ItemTransferred))]
    internal static class NativeTransferPatch
    {
        // Postfix OBSERVES transfers. The native game remains authoritative.
        private static void Postfix(WorldFlowPipe __instance, InventoryItem item)
        {
            try { FoundationMachineApi.Telemetry.OnTransfer(__instance?.Pipe); }
            catch (Exception ex) { MachineRuntime.ReportCallbackError("Native transfer observer", ex); }
        }
    }
}
