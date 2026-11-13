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
    [HarmonyPatch(typeof(SpriteLibrary), nameof(SpriteLibrary.Get), new[] { typeof(string) })]
    internal static class CustomIcon
    {
        static bool Prefix(string name, ref Sprite __result)
        {
            if (!NativeResearch.TryIcon(name, out var sprite))
                return true;
            __result = sprite;
            return false;
        }
    }
}
