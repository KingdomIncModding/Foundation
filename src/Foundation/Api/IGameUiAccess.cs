using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KingdomFoundation.Api
{
    /// <summary>Native UI/input primitives without exposing game classes or private field names.</summary>
    public interface IGameUiAccess
    {
        Component Root { get; }
        Component OptionsTemplate { get; }
        System.Collections.Generic.IReadOnlyList<Component> PopupTemplates { get; }
        TMP_Text ShowStatusText(string text,string iconName,bool highlight=false);
        RectTransform WorldOverlayTemplate { get; }
        GameObject CloneButtonTemplate { get; }
        RectTransform EditToolbar { get; }
        Component ItemFilterTemplate {get;}
        Component WorkspaceSwitcher {get;}
        Component WorkspaceItemTemplate {get;}
        Image WorkspaceItemIcon(Component item);
        TMP_Text WorkspaceItemIndex(Component item);
        GameObject WorkspaceItemHighlight(Component item);
        void RemoveNativeTooltips(GameObject source);
        bool IsMenuVisible { get; }
        bool HasActiveGameMenu {get;}
        Vector2Int MouseWorld {get;}
        Vector2 MouseUi {get;}
        void ShowPointerWarning(string message);
        void ShowInsufficientFunds();
        void RefreshBuildBar();
        bool IsGameInputEnabled { get; }
        bool IsTutorialActive { get; }
        bool InteractReleased { get; }
        bool IsDraggingGhost { get; }
        void CloseAllMenus();
        void ShowStatusMessage(string text);
        bool IsWorkspaceVisible { get; }
        bool IsDeconstructionVisible { get; }
        bool IsAlertVisible { get; }
        bool HasPopup { get; }
        float PointerTraverseDistance { get; }
        bool IsPointerOverResearchList { get; }
        bool PointerHasNativeRightClickAction { get; }
        bool IsDragDropReady { get; }
        bool IsDragDropBusy { get; }
        bool IsPointerOverButton { get; }
        bool IsPointerOverPort { get; }
        bool IsPointerOverNonWorkspaceUi { get; }
        GameDeconstructionMode DeconstructionMode { get; }
        bool DeconstructionHeld { get; }
        void SetDeconstructionVisible(bool visible);
        bool IsMenuOpen(GameMenu menu);
        void SetMenuVisible(GameMenu menu, bool visible);
        bool CancelDragDrop();
        void ProcessResearchEscape();
        void UpdateMouseWorld();
        void SetGameInputEnabled(bool enabled);
        void SetUiVisible(bool visible);
        void RemoveNativeKeyBindingTooltip(GameObject view);
        void ShowFloatingIconText(Vector2 position, Sprite icon, string message);
        bool IsPointerOverHud(Component ignoredView = null);
        void SetDeconstructionRefund(Component overlay, GameMoney amount);
        void HideDeconstructionRefund(Component overlay);
        void PlaySound(GameUiSound sound);
        Sprite CurrencyIcon(string currencyId, int denomination = 0);
        string FormatNumber(long value);
    }
}
