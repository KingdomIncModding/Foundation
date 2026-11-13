using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    internal static class SaveIntegration
    {
        internal static void Install(Harmony hooks)
        {
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.SaveCurrentStatePatch)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.LoadStatePatch)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.NewPlayerPatch)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.WorldReadyPatch)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.WorldLeavingPatch)).Patch();
        }
    }
}
