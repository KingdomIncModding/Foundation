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
    [HarmonyPatch(typeof(TechTreeMenu), "OnDisable")]
    internal static class CloseTree
    {
        static void Prefix(TechTreeMenu __instance) => global::KingdomFoundation.FoundationPlugin.Tabs(__instance)?.Close();
    }
}
