using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    /// <summary>Extends the verified native SaveGame JSON pipeline; never writes to a second file.</summary>
    internal static partial class SaveIntegration
    {
        internal static void Install(Harmony hooks)
        {
            hooks.CreateClassProcessor(typeof(SaveCurrentStatePatch)).Patch();
            hooks.CreateClassProcessor(typeof(LoadStatePatch)).Patch();
            hooks.CreateClassProcessor(typeof(NewPlayerPatch)).Patch();
            hooks.CreateClassProcessor(typeof(WorldReadyPatch)).Patch();
            hooks.CreateClassProcessor(typeof(WorldLeavingPatch)).Patch();
        }





    }
}
