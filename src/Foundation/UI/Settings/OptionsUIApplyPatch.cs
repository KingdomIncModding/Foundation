using Assets.Behaviour.UI;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine.UI;

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
                ingameField.SetValue(__instance, __instance.GetComponentInParent<InGameMenuUI>() ?? global::UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>());
            var menu = __instance.GetComponent<FoundationConfigMenu>();
            menu?.CommitModConfigs();
            if (menu != null)
            {
                // If a mod hid the vanilla volume row and manages base volume itself, keep OptionsUI from overwriting it with a stale slider value.
                if (menu.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var musicRow) && !musicRow.IsVisible)
                    SyncSlider(__instance, "_musicSlider", MusicManager.Volume);
                if (menu.TryGetVanillaSetting(VanillaSettingIds.SoundVolume, out var soundRow) && !soundRow.IsVisible)
                    SyncSlider(__instance, "_soundSlider", UISounds.Volume);
            }
        }

        static void SyncSlider(OptionsUI options, string fieldName, float value)
        {
            var slider = AccessTools.Field(typeof(OptionsUI), fieldName)?.GetValue(options) as Slider;
            if (slider != null) slider.SetValueWithoutNotify(value);
        }
    }
}
