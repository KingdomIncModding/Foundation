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
    // Mods has its own camera state; keep the vanilla saved position on Base Game.
    [HarmonyPatch(typeof(TechTreeMenu), "Update")]
    internal static class TreeUpdate
    {
        static bool Prefix() => !global::KingdomFoundation.FoundationPlugin.ModsOpen;
    }
}
