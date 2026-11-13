using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;

namespace KingdomFoundation.Api
{
    /// <summary>Post-commit observation of native GameUI.TogglePopupMenu.</summary>
    public sealed class NativePopupVisibilityEvent
    {
        public GameUI Ui { get; }
        public UIPopupMenu Popup { get; }
        public bool IsVisible { get; }
        public NativePopupVisibilityEvent(GameUI ui, UIPopupMenu popup, bool visible)
        { Ui = ui; Popup = popup; IsVisible = visible; }
    }
}
