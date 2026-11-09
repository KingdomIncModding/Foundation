using Assets.Behaviour.UI.MainMenu;
using HarmonyLib;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(OptionsUI), "OnEnable")]
    internal static class OptionsUIOnEnablePatch
    {
        static void Postfix(OptionsUI __instance)
        {
            var menu = FoundationConfigMenu.Attach(__instance);
        }
    }
}
