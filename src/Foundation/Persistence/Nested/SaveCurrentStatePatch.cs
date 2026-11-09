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
            // Recalculate after other common save postfixes when possible.
            [HarmonyPriority(Priority.Last)]
            static void Postfix(JsonObject __result)
            {
                if (__result == null) return;
                try
                {
                    FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.BeforeSave, GamePlayer.Current);
                    FoundationApi.SaveDataInternal.WriteState(__result);
                    MissingResearchRecovery.Extend(__result);
                    // Native SaveCurrentState calculates Secret BEFORE this postfix.
                    // Recalculate its FNV digest over the complete extended document.
                    __result.Remove("Secret");
                    __result["Secret"] = new SeedGenerator().Add(__result).Hash.ToString();
                }
                catch (Exception exception)
                {
                    // Do not block the native save when a framework extension fails.
                    // Restore the vanilla document shape and vanilla checksum.
                    FoundationApi.Report(new InvalidOperationException(
                        "Foundation save extension failed; writing vanilla save without mod data", exception));
                    try
                    {
                        __result.Remove(ModSaveRegistry.RootKey);
                        MissingResearchRecovery.Extend(__result);
                        __result.Remove("Secret");
                        __result["Secret"] = new SeedGenerator().Add(__result).Hash.ToString();
                    }
                    catch (Exception recoveryError)
                    {
                        FoundationApi.Report(new InvalidOperationException(
                            "Could not recover native save checksum after a Foundation extension error", recoveryError));
                    }
                }
            }
        }
    }

}
