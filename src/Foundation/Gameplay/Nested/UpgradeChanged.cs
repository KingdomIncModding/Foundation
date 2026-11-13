using System;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.SetUpgrade))]
    internal static class UpgradeChanged
    {
        static void Prefix(FlowElement __instance, int idx, out FlowUpgrade __state) => __state = __instance.GetUpgrade(idx);
        static void Postfix(FlowElement __instance, int idx, FlowUpgrade __state)
        {
            var now = __instance.GetUpgrade(idx);
            if (__state != now)
                global::KingdomFoundation.GameplayIntegration.Publish(new MachineUpgradeChangedEvent(__instance, idx, __state, now));
        }
    }
}
