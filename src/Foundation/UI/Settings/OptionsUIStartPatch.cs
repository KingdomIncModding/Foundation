using Assets.Behaviour.UI.MainMenu;
using HarmonyLib;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(OptionsUI), "Start")]
    internal static class OptionsUIStartPatch
    {
        static void Postfix(OptionsUI __instance)
        {
            FoundationConfigMenu.Attach(__instance);
        }
    }
}
