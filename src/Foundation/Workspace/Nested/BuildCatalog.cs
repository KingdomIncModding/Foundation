using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LightJson;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.GetAllTypes))]
    internal static class BuildCatalog
    {
        static void Postfix(ref IEnumerable<FlowElement> __result) => __result = WorkspaceNodeRegistry.Shared.Include(__result);
    }
}
