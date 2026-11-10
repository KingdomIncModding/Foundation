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
        [HarmonyPatch]
        static class ListResearch
        {
            static MethodBase TargetMethod() => AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(TechTreeListView), "_addNodes", Type.EmptyTypes));
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => FilterCatalog(instructions);
        }
    }

}
