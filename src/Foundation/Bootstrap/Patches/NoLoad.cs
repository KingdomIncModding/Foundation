using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))] static class NoLoad { static bool Prefix(ref bool __result) { __result = false; return false; } }
    }

}

