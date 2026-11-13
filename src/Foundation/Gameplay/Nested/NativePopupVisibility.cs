using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(GameUI), nameof(GameUI.TogglePopupMenu), new[] { typeof(UIPopupMenu), typeof(bool), typeof(bool) })]
    internal static class NativePopupVisibility
    {
        private sealed class Before
        {
            internal UIPopupMenu Other;
            internal bool OtherVisible;
            internal bool RequestedVisible;
        }

        static void Prefix(GameUI __instance, UIPopupMenu popup, out global::KingdomFoundation.NativePopupVisibility.Before __state)
        {
            var field = HarmonyLib.AccessTools.Field(typeof(GameUI), "_currentPopup");
            var previous = field == null ? null : field.GetValue(__instance) as UIPopupMenu;
            __state = new global::KingdomFoundation.NativePopupVisibility.Before
            {
                Other = previous != popup ? previous : null,
                OtherVisible = previous && previous.gameObject.activeSelf,
                RequestedVisible = popup && popup.gameObject.activeSelf
            };
        }

        static void Postfix(GameUI __instance, UIPopupMenu popup, global::KingdomFoundation.NativePopupVisibility.Before __state)
        {
            if (__state != null && __state.Other && __state.OtherVisible && !__state.Other.gameObject.activeSelf)
                global::KingdomFoundation.GameEventIntegration.Publish(new NativePopupVisibilityEvent(__instance, __state.Other, false));
            if (!popup)
                return;
            bool now = popup.gameObject.activeSelf;
            if (__state == null || now != __state.RequestedVisible)
                global::KingdomFoundation.GameEventIntegration.Publish(new NativePopupVisibilityEvent(__instance, popup, now));
        }
    }
}
