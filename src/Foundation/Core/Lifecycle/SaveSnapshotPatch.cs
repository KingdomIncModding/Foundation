using Assets.Source.Util;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.SaveCurrentState))]
    internal static class SaveSnapshotPatch
    {
        static void Postfix() => FoundationCoreApi.Runtime.Publish(SaveSnapshotBuiltEvent.Instance);
    }
}
