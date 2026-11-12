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
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.SaveCurrentState))]
        private static class SaveCurrentStatePatch
        {
            // Extend the document after other common save postfixes when possible.
            [HarmonyPriority(Priority.Last)]
            static void Postfix(JsonObject __result)
            {
                if (__result == null) return;
                bool hadNativeChecksum = __result.ContainsKey("Secret");
                try
                {
                    FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.BeforeSave, GamePlayer.Current);
                    FoundationApi.SaveDataInternal.WriteState(__result);
                    MissingResearchRecovery.Extend(__result);
                    // Game 1.0.14 no longer writes Secret. Never introduce a checksum
                    // into new saves; retain the adapter only for legacy documents.
#pragma warning disable CS0618 // Explicitly retained game 1.0.13 compatibility; remove next release.
                    if (hadNativeChecksum) LegacySaveChecksum.Refresh(__result);
#pragma warning restore CS0618
                }
                catch (Exception exception)
                {
                    // Do not block the native save when a framework extension fails.
                    // Restore the native document shape, retaining missing research.
                    FoundationApi.Report(new InvalidOperationException(
                        "Foundation save extension failed; writing vanilla save without mod data", exception));
                    try
                    {
                        __result.Remove(ModSaveRegistry.RootKey);
                        MissingResearchRecovery.Extend(__result);
#pragma warning disable CS0618 // Explicitly retained game 1.0.13 compatibility; remove next release.
                        if (hadNativeChecksum) LegacySaveChecksum.Refresh(__result);
#pragma warning restore CS0618
                    }
                    catch (Exception recoveryError)
                    {
                        FoundationApi.Report(new InvalidOperationException(
                            "Could not recover native save document after a Foundation extension error", recoveryError));
                    }
                }
            }
        }
    }

}
