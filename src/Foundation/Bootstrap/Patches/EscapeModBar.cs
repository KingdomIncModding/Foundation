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
        [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), nameof(Assets.Behaviour.UI.GameUI.ProcessEscape))]
        static class EscapeModBar
        { static bool Prefix(Assets.Behaviour.UI.GameUI __instance) => !__instance.GetComponentsInChildren<ModBarHost>(true).Any(host => host.ClosePopups()); }
    }

}
