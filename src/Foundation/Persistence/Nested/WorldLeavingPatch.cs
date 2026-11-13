using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(Assets.Behaviour.GameManager), "OnApplicationQuit")]
    internal static class WorldLeavingPatch
    {
        static void Prefix() => FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.GameLeaving, GamePlayer.Current);
    }
}
