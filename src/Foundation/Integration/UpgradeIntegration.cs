using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class UpgradeIntegration
    {
        internal static void Install(Harmony harmony) => harmony.CreateClassProcessor(typeof(CreateNativeUpgrade)).Patch();

    }
}
