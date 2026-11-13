using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowPipe), nameof(FlowPipe.Update))]
    internal static class PauseMissingPipe
    {
        static bool Prefix(FlowPipe __instance) => !(__instance.ConnectedOutput?.Parent is MissingWorkspaceElement || __instance.ConnectedInput?.Parent is MissingWorkspaceElement);
    }
}
