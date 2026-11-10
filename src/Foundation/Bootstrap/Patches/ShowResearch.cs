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
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.ShowTechTree))]
        static class ShowResearch
        {
            static void Prefix(TechTreeMenu __instance, ref TechNode root)
            {
                var tabs = Tabs(__instance);
                if (!tabs || tabs.Switching || root == null) return;
                if (NativeResearch.Owns(root) && !NativeResearch.Active(root)) root = null;
                else tabs.SelectFor(root);
            }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => FilterCatalog(instructions);
            static void Postfix(TechTreeMenu __instance) { if (ModsOpen) ModResearchLayout.Arrange(__instance); }
        }
    }

}
