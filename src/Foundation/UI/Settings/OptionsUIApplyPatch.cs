using Assets.Behaviour.UI.MainMenu;
using HarmonyLib;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.ApplyAndClose))]
    internal static class OptionsUIApplyPatch
    {
        static void Prefix(OptionsUI __instance)
        {
            var parentField = AccessTools.Field(typeof(OptionsUI), "_parent");
            var ingameField = AccessTools.Field(typeof(OptionsUI), "_ingameParent");
            if (parentField != null && parentField.GetValue(__instance) == null)
                parentField.SetValue(__instance, __instance.GetComponentInParent<MainMenuUI>() ?? MainMenuUI.Instance);
            if (ingameField != null && ingameField.GetValue(__instance) == null)
                ingameField.SetValue(__instance, __instance.GetComponentInParent<InGameMenuUI>() ?? UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>());

            __instance.GetComponent<FoundationConfigMenu>()?.CommitModConfigs();
        }
    }
}
