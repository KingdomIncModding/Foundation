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
        // Defense in depth: never write any native save file in smoke mode,
        // including autosave-exit on normal application shutdown.
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))]
        private static class BlockAnyNativeSaveInSmoke
        {
            static bool Prefix() => false;
        }
    }

}
