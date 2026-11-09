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
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadState))]
        private static class LoadStatePatch
        {
            static void Prefix(JsonObject data, out MissingResearchRecovery.LoadSnapshot __state)
            {
                // The input has already passed the native SaveGameFile checksum
                // validation. Strip only unavailable notification keys from the
                // in-memory structure before GamePlayer.FromJson rejects them.
                MissingResearchRecovery.LoadSnapshot captured = null;
                try { captured = MissingResearchRecovery.PrepareLoad(data); }
                catch (Exception exception) { FoundationApi.Report(exception); }
                __state = captured;
            }
            static void Postfix(JsonObject data, MissingResearchRecovery.LoadSnapshot __state)
            {
                if (__state != null) MissingResearchRecovery.Loaded(__state);
                try { FoundationApi.SaveDataInternal.LoadState(data); }
                catch (Exception exception) { FoundationApi.Report(exception); }
                FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.SaveLoaded, GamePlayer.Current);
            }
            static Exception Finalizer(Exception __exception, JsonObject data, MissingResearchRecovery.LoadSnapshot __state)
            {
                if (__exception != null)
                    try { MissingResearchRecovery.RestoreInputOnFailure(data, __state); }
                    catch (Exception restoreError) { FoundationApi.Report(restoreError); }
                return __exception;
            }
        }
    }

}
