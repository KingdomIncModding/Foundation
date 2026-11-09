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
    internal static partial class WorkspaceIntegration
    {
        [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.ToJson))]
        static class PreserveMissingJson
        {
            static void Postfix(FlowElement __instance, ref LightJson.JsonValue __result)
            {
                if (__instance is MissingWorkspaceElement missing && __result.IsJsonObject)
                    missing.PreservePortFields(__result.AsJsonObject);
            }
        }
    }

}
