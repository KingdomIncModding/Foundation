using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowCamera), nameof(WorldFlowCamera.RecalculateBounds))]
    internal static class EmptyBounds
    {
        static void Postfix(WorldFlowCamera __instance)
        {
            if (!global::KingdomFoundation.FoundationPlugin.ModsOpen || TechTreeMenu.Instance.Nodes.Any())
                return;
            foreach (var name in new[]
            {
                "xMin",
                "yMin"
            }

            )
                AccessTools.Field(typeof(WorldFlowCamera), name).SetValue(__instance, -1000f);
            foreach (var name in new[]
            {
                "xMax",
                "yMax"
            }

            )
                AccessTools.Field(typeof(WorldFlowCamera), name).SetValue(__instance, 1000f);
        }
    }
}
