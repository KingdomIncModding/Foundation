using System;
using System.Collections.Generic;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.SaveCurrentState))]
    internal static class SaveCurrentStatePatch
    {
        // Allows isolated Foundation tests to verify failure recovery after WriteState.
        internal static Action<JsonObject> AfterExtensionHookForTest;
        internal static void ApplyForTest(JsonObject result) => Postfix(result);

        // Extend the document after other common save postfixes when possible.
        [HarmonyPriority(Priority.Last)]
        static void Postfix(JsonObject __result)
        {
            if (__result == null)
                return;
            bool hadNativeChecksum = __result.ContainsKey("Secret");
            var previousRoot = __result.ContainsKey(ModSaveRegistry.RootKey)
                ? ModSaveRegistry.Clone(__result[ModSaveRegistry.RootKey])
                : JsonValue.Null;
            var saves = FoundationApi.SaveDataInternal;
            bool hadKnownGoodModData = !previousRoot.IsNull || (saves != null && saves.TryBuildRetainedPayload(out _));
            try
            {
                FoundationApi.LifecycleEvents.Raise(FoundationLifecyclePhase.BeforeSave, GamePlayer.Current);
                saves.WriteState(__result);
                MissingResearchRecovery.Extend(__result);
                AfterExtensionHookForTest?.Invoke(__result);
                // Game 1.0.14 no longer writes Secret. Never introduce a checksum
                // into new saves; retain the adapter only for legacy documents.
#pragma warning disable CS0618 // Explicitly retained game 1.0.13 compatibility; remove next release.

                if (hadNativeChecksum)
                    LegacySaveChecksum.Refresh(__result);
#pragma warning restore CS0618
            }
            catch (Exception exception)
            {
                FoundationApi.Report(new InvalidOperationException("Foundation save extension failed; preserving known-good mod save data.", exception));
                bool preservedModData = false;
                try
                {
                    if (saves != null && saves.TryBuildRetainedPayload(out var retainedPayload) && !retainedPayload.IsNull)
                    {
                        __result[ModSaveRegistry.RootKey] = retainedPayload;
                        preservedModData = true;
                    }
                    else if (!previousRoot.IsNull)
                    {
                        __result[ModSaveRegistry.RootKey] = previousRoot;
                        preservedModData = true;
                    }

                    MissingResearchRecovery.Extend(__result);
#pragma warning disable CS0618 // Explicitly retained game 1.0.13 compatibility; remove next release.

                    if (hadNativeChecksum)
                        LegacySaveChecksum.Refresh(__result);
#pragma warning restore CS0618
                }
                catch (Exception recoveryError)
                {
                    FoundationApi.Report(new InvalidOperationException("Could not recover mod save data after a Foundation extension error.", recoveryError));
                    throw new InvalidOperationException("Save aborted: could not preserve Foundation mod data after a save extension failure.", recoveryError);
                }

                if (hadKnownGoodModData && !preservedModData)
                    throw new InvalidOperationException("Save aborted: refusing to overwrite a modded save without known-good Foundation mod data.", exception);
            }
        }
    }
}
