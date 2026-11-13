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
    [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), nameof(Assets.Behaviour.UI.GameUI.ToggleUI))]
    internal static class HideModBar
    {
        static void Postfix(Assets.Behaviour.UI.GameUI __instance)
        {
            if (!__instance.GetComponent<Canvas>().enabled)
                foreach (var host in __instance.GetComponentsInChildren<ModBarHost>(true))
                    host.ClosePopups();
        }
    }
}
