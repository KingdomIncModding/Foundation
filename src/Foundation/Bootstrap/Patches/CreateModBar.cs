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
    [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), "Start")]
    internal static class CreateModBar
    {
        static void Postfix(Assets.Behaviour.UI.GameUI __instance) => ModBarHost.Attach(__instance);
    }
}
