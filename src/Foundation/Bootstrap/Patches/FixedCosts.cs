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
    public sealed partial class FoundationPlugin
    {
        [HarmonyPatch(typeof(TechNode), nameof(TechNode.ResetCost))]
        static class FixedCosts { static void Postfix(TechNode __instance) => NativeResearch.RestoreFixedCost(__instance); }
    }

}
