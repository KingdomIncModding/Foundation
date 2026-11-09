using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class UpgradeIntegration
    {
        [HarmonyPatch(typeof(FlowUpgrade), nameof(FlowUpgrade.Create))]
        private static class CreateNativeUpgrade
        {
            // All other upgrade types follow the game's original factory unchanged.
            static bool Prefix(string name, ref FlowUpgrade __result)
            {
                if (!FoundationApi.Upgrades.TryCreate(name, out var upgrade)) return true;
                __result = upgrade;
                return false;
            }
        }
    }

}
