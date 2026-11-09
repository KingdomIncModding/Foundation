using Assets.Source.Util;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))]
    internal static class SaveStorePatch
    {
        static void Postfix(string saveName) => FoundationCoreApi.Runtime.Publish(new SaveStoredEvent(saveName));
    }
}
