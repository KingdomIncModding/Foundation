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
    [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.ShowTechTree))]
    internal static class ShowResearch
    {
        static void Prefix(TechTreeMenu __instance, ref TechNode root)
        {
            var tabs = global::KingdomFoundation.FoundationPlugin.Tabs(__instance);
            if (!tabs || tabs.Switching || root == null)
                return;
            if (NativeResearch.Owns(root) && !NativeResearch.Active(root))
                root = null;
            else
                tabs.SelectFor(root);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => global::KingdomFoundation.FoundationPlugin.FilterCatalog(instructions);
        static void Postfix(TechTreeMenu __instance)
        {
            if (global::KingdomFoundation.FoundationPlugin.ModsOpen)
                ModResearchLayout.Arrange(__instance);
        }
    }
}
