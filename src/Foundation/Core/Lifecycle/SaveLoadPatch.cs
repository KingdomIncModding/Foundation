using System;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadState))]
    internal static class SaveLoadPatch
    {
        static void Prefix() => FoundationCoreApi.Runtime.Publish(SaveLoadingEvent.Instance);
        static void Postfix() => FoundationCoreApi.Runtime.Publish(SaveLoadedEvent.Instance);
        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null) FoundationCoreApi.Runtime.Publish(new SaveLoadFailedEvent(__exception));
            return __exception; // preserve native failure behavior
        }
    }
}
