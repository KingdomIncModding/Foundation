using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    internal static partial class SaveIntegration
    {
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.CreateNewPlayer))]
        private static class NewPlayerPatch
        {
            static void Postfix(GamePlayer __result)
            {
                MissingResearchRecovery.Clear();
                try { FoundationApi.SaveDataInternal.NewKingdom(); }
                catch (Exception exception) { FoundationApi.Report(exception); }
                FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.NewKingdom, __result);
            }
        }
    }

}
