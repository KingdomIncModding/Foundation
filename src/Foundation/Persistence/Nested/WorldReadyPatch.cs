using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(Assets.Behaviour.GameManager), "Start")]
    internal static class WorldReadyPatch
    {
        static void Postfix() => FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.GameReady, GamePlayer.Current);
    }
}
