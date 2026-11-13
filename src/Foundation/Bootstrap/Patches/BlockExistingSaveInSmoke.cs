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
    // Block reading any existing user autosave while in diagnostic mode.
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))]
    internal static class BlockExistingSaveInSmoke
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
