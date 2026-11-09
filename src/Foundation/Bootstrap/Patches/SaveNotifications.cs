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
        // Vanilla ignores missing researched IDs on load, but its NewTechCount dictionary
        // cannot accept missing keys. Keep Foundation's branch counters session-only.
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.ToJson))]
        static class SaveNotifications
        {
            static void Postfix(JsonValue __result)
            {
                var counts = __result["NewTechCount"].AsJsonObject;
                foreach (var node in TechNode.Nodes.Where(NativeResearch.Owns)) counts.Remove(node.Identifier);
            }
        }
    }

}
