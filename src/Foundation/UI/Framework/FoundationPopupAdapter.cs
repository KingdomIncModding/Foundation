using Assets.Behaviour.UI.Shared;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationPopupAdapter : UIPopupMenu
    {
        internal FoundationPopup Target;
        public override bool CanCancel=>!Target || Target.CanCancel;
        public override bool CanEscape=>!Target || Target.CanEscape;
        public override void PopupClosed(){if(Target)Target.PopupClosed();base.PopupClosed();}
    }
}
