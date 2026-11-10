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
        [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.FocusNode))]
        static class FocusResearch
        {
            static bool Prefix(TechTreeMenu __instance, TechNode node)
            {
                Tabs(__instance)?.SelectFor(node);
                // Native focus assumes a zero-position node has a parent. Framework
                // roots may use the default origin, so handle that valid case here.
                if (node.Root != null || node.Position != Vector2.zero) return true;
                Assets.Behaviour.UI.UISounds.WindowOpen();
                __instance.ShowTechTree(null, true);
                TechTreeTabs.Field<Camera>(__instance, "_camera").transform.position = new Vector3(node.Position.x, node.Position.y, -10);
                return false;
            }
        }
    }

}
