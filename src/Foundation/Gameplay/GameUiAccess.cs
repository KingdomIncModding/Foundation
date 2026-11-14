using System;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.UI.Farm;
using Assets.Behaviour.UI.SwapFlow;
using Assets.Behaviour.UI.TechTree;
using Assets.Behaviour.UI.WarEffort;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameUiAccess : IGameUiAccess
    {
        public bool HasActiveGameMenu => GameUI.Instance && System.Linq.Enumerable.Any((GameUIType[])Enum.GetValues(typeof(GameUIType)), menu => GameUI.Instance.IsActive(menu));
        public Vector2Int MouseWorld => Assets.Behaviour.Input.PlayerInput.MouseWorld;
        public Vector2 MouseUi => Assets.Behaviour.Input.PlayerInput.MouseUI;

        public void ShowPointerWarning(string message) => GameUI.Instance.ShowWarning(Assets.Behaviour.Input.PlayerInput.MouseUI, message);
        public void ShowInsufficientFunds() => GameUI.Instance.InsufficientFundsWarning();
        public void RefreshBuildBar() => GameUI.Instance.UpdateBuildBarButtons();
        public Component OptionsTemplate => System.Linq.Enumerable.FirstOrDefault(Resources.FindObjectsOfTypeAll<OptionsUI>());
        public System.Collections.Generic.IReadOnlyList<Component> PopupTemplates => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<Component>(Resources.FindObjectsOfTypeAll<UIPopupMenu>()));

        public TMPro.TMP_Text ShowStatusText(string text, string iconName, bool highlight = false) => UIStatusMessage.Show(text, iconName, highlight)?.StatusText;
        public bool IsGameInputEnabled => Assets.Behaviour.Input.PlayerInput.Enabled;
        public bool IsTutorialActive => TutorialUI.Active;
        public bool InteractReleased => Assets.Behaviour.Input.PlayerInput.InteractReleased;
        public bool IsDraggingGhost => DragDropManager.Instance && DragDropManager.Instance.Target is WorldFlowGhost;

        public void CloseAllMenus() => GameUI.Instance.CloseAllExcept(null);
        public void ShowStatusMessage(string text) => UIStatusMessage.Show(text, "Icons_64", false);
        private static readonly AccessTools.FieldRef<GameUI, AreaCopyGhost> CloneGhost = AccessTools.FieldRefAccess<GameUI, AreaCopyGhost>("_cloneGhost");
        private static readonly AccessTools.FieldRef<GameUI, GameObject> CloneButton = AccessTools.FieldRefAccess<GameUI, GameObject>("_cloneMenuButton");
        private static readonly AccessTools.FieldRef<GameUI, RectTransform> Toolbar = AccessTools.FieldRefAccess<GameUI, RectTransform>("_editHotbar");
        private static readonly AccessTools.FieldRef<GameUI, SwapFlowMenu> FlowMenu = AccessTools.FieldRefAccess<GameUI, SwapFlowMenu>("_flowMenu");
        private static readonly AccessTools.FieldRef<SwapFlowMenu, SwapFlowItem> FlowItem = AccessTools.FieldRefAccess<SwapFlowMenu, SwapFlowItem>("_itemPrefab");
        private static readonly AccessTools.FieldRef<SwapFlowItem, UnityEngine.UI.Image> FlowIcon = AccessTools.FieldRefAccess<SwapFlowItem, UnityEngine.UI.Image>("_icon");
        private static readonly AccessTools.FieldRef<SwapFlowItem, TMPro.TMP_Text> FlowIndex = AccessTools.FieldRefAccess<SwapFlowItem, TMPro.TMP_Text>("_index");
        private static readonly AccessTools.FieldRef<SwapFlowItem, GameObject> FlowHighlight = AccessTools.FieldRefAccess<SwapFlowItem, GameObject>("_highlight");
        private static readonly AccessTools.FieldRef<DeconstructOverlay, RectTransform> RefundContainer = AccessTools.FieldRefAccess<DeconstructOverlay, RectTransform>("_refundContainer");
        private static readonly AccessTools.FieldRef<DeconstructOverlay, CurrencyDisplay> RefundDisplay = AccessTools.FieldRefAccess<DeconstructOverlay, CurrencyDisplay>("_refund");
        private static readonly AccessTools.FieldRef<CurrencyDisplay, Sprite> CopperIcon = AccessTools.FieldRefAccess<CurrencyDisplay, Sprite>("_copperIcon");
        private static readonly AccessTools.FieldRef<CurrencyDisplay, Sprite> SilverIcon = AccessTools.FieldRefAccess<CurrencyDisplay, Sprite>("_silverIcon");
        private static readonly AccessTools.FieldRef<GameUI, UIPopupMenu> Popup = AccessTools.FieldRefAccess<GameUI, UIPopupMenu>("_currentPopup");
        private static readonly AccessTools.FieldRef<GameUI, TechTreeMenu> ResearchMenu = AccessTools.FieldRefAccess<GameUI, TechTreeMenu>("_techMenu");
        private static readonly Type[] RightClickConsumers =
        {
            typeof(CurrencyCell),
            typeof(FarmMenuPlot),
            typeof(WarEffortSoldierSprite),
            typeof(SwapFlowItem),
            typeof(RightClickAction),
            typeof(WorldFlowIO),
            typeof(WorldFlowPipeNode),
            typeof(Assets.Behaviour.Flow.Elements.WorldFlowArenaSlot),
            typeof(Assets.Behaviour.Flow.Elements.WorldFlowTrainingGroundsSlot)
        };
        public Component Root => GameUI.Instance;
        public RectTransform WorldOverlayTemplate => GameUI.Instance ? CloneGhost(GameUI.Instance)?.GetComponent<RectTransform>() : null;
        public GameObject CloneButtonTemplate => GameUI.Instance ? CloneButton(GameUI.Instance) : null;
        public RectTransform EditToolbar => GameUI.Instance ? Toolbar(GameUI.Instance) : null;
        public RectTransform ModBarBounds => GameUI.Instance ? GameUI.Instance.GetComponentInChildren<global::KingdomFoundation.ModBarHost>(true)?.Clip : null;
        public Component ItemFilterTemplate => GameUI.Instance ? GameUI.Instance.FilterPopup : null;
        public Component WorkspaceSwitcher => GameUI.Instance ? FlowMenu(GameUI.Instance) : null;
        public Component WorkspaceItemTemplate => GameUI.Instance ? FlowItem(FlowMenu(GameUI.Instance)) : null;

        private static SwapFlowItem RequireWorkspaceItem(Component view) => view is SwapFlowItem item ? item : throw new ArgumentException("Expected a native workspace item template.", nameof(view));
        public UnityEngine.UI.Image WorkspaceItemIcon(Component item) => FlowIcon(RequireWorkspaceItem(item));
        public TMPro.TMP_Text WorkspaceItemIndex(Component item) => FlowIndex(RequireWorkspaceItem(item));
        public GameObject WorkspaceItemHighlight(Component item) => FlowHighlight(RequireWorkspaceItem(item));
        public void RemoveNativeTooltips(GameObject source)
        {
            if (source)
                foreach (var tooltip in source.GetComponents<Behaviour.UI.Tooltip.TooltipSource>())
                    global::UnityEngine.Object.Destroy(tooltip);
        }

        public bool IsMenuVisible => GameUI.MenuVisible;
        public bool IsWorkspaceVisible => WorldFlowManager.Visible && WorldFlowManager.Instance;
        public bool IsDeconstructionVisible => GameUI.Instance && GameUI.Instance.IsActive(GameUIType.Deconstruct);
        public bool IsAlertVisible => UIAlertWindow.Active;
        public bool HasPopup => GameUI.Instance && Popup(GameUI.Instance) != null;
        public bool IsDragDropReady => DragDropManager.Instance;
        public bool IsDragDropBusy => DragDropManager.Instance && DragDropManager.Instance.Busy;
        public bool IsPointerOverButton => UnityEngine.EventSystems.EventSystem.current != null && UIHelper.GetMouseOverUI<UnityEngine.UI.Button>();
        public bool IsPointerOverPort => UnityEngine.EventSystems.EventSystem.current != null && UIHelper.GetMouseOverUI<WorldFlowIO>();

        public bool IsPointerOverNonWorkspaceUi
        {
            get
            {
                if (UnityEngine.EventSystems.EventSystem.current == null)
                    return false;
                foreach (var hit in UIHelper.GetAllMouseOverUI())
                    if (hit && !hit.GetComponentInParent<WorldFlowElement>() && !hit.GetComponentInParent<WorldFlowDeco>() && !hit.GetComponentInParent<WorldFlowPipe>())
                        return true;
                return false;
            }
        }

        public void UpdateMouseWorld()
        {
            if (Assets.Behaviour.Input.PlayerInput.Instance)
                Assets.Behaviour.Input.PlayerInput.Instance.UpdateMouseWorld();
        }

        public void SetGameInputEnabled(bool enabled)
        {
            if (enabled)
                Assets.Behaviour.Input.PlayerInput.Enable();
            else
                Assets.Behaviour.Input.PlayerInput.Disable();
        }

        public void SetUiVisible(bool visible)
        {
            if (GameUI.Instance)
                GameUI.Instance.ToggleUI(visible);
        }

        public void RemoveNativeKeyBindingTooltip(GameObject view)
        {
            if (view)
            {
                var tooltip = view.GetComponent<Assets.Behaviour.Util.KeyBindingTooltip>();
                if (tooltip)
                    global::UnityEngine.Object.Destroy(tooltip);
            }
        }

        public void ShowFloatingIconText(Vector2 position, Sprite icon, string message)
        {
            if (WorldFlowManager.Instance)
                WorldFlowManager.Instance.ShowFloatingIconText(position, icon, message);
        }

        public float PointerTraverseDistance => Assets.Behaviour.Input.PlayerInput.Instance ? Assets.Behaviour.Input.PlayerInput.Instance.TraverseDistance : 0;
        public bool IsPointerOverResearchList => UnityEngine.EventSystems.EventSystem.current != null && UIHelper.GetMouseOverUI<TechTreeListItem>() != null;

        public bool PointerHasNativeRightClickAction
        {
            get
            {
                if (UnityEngine.EventSystems.EventSystem.current == null)
                    return false;
                foreach (var hit in UIHelper.GetAllMouseOverUI())
                    foreach (var type in RightClickConsumers)
                        if (hit && hit.GetComponentInParent(type) != null)
                            return true;
                return false;
            }
        }

        public bool CancelDragDrop() => DragDropManager.Instance && DragDropManager.Instance.CancelDragDrop();
        public void ProcessResearchEscape()
        {
            if (GameUI.Instance && ResearchMenu(GameUI.Instance))
                ResearchMenu(GameUI.Instance).ProcessEscape();
        }

        private static GameUIType NativeMenu(GameMenu menu)
        {
            switch (menu)
            {
                case GameMenu.Build:
                    return GameUIType.BuyMenu;
                case GameMenu.Items:
                    return GameUIType.ItemMenu;
                case GameMenu.Research:
                    return GameUIType.Tech;
                case GameMenu.Resources:
                    return GameUIType.Resources;
                case GameMenu.Decorations:
                    return GameUIType.Deco;
                case GameMenu.War:
                    return GameUIType.WarEffort;
                case GameMenu.Forest:
                    return GameUIType.Forest;
                case GameMenu.Farm:
                    return GameUIType.Farm;
                case GameMenu.Dungeons:
                    return GameUIType.Dungeon;
                case GameMenu.Deconstruction:
                    return GameUIType.Deconstruct;
                case GameMenu.Clone:
                    return GameUIType.Clone;
                case GameMenu.AreaMove:
                    return GameUIType.AreaMove;
                default:
                    throw new ArgumentOutOfRangeException(nameof(menu));
            }
        }

        public bool IsMenuOpen(GameMenu menu) => GameUI.Instance && GameUI.Instance.IsActive(NativeMenu(menu));
        public void SetMenuVisible(GameMenu menu, bool visible)
        {
            switch (menu)
            {
                case GameMenu.Build:
                    FoundationApi.UI.ShowBuildMenu(visible);
                    break;
                case GameMenu.Items:
                    FoundationApi.UI.ShowItems(visible);
                    break;
                case GameMenu.Research:
                    FoundationApi.UI.ShowResearch(visible);
                    break;
                case GameMenu.Resources:
                    FoundationApi.UI.ShowResources(visible);
                    break;
                case GameMenu.Decorations:
                    FoundationApi.UI.ShowDecorationMenu(visible);
                    break;
                case GameMenu.War:
                    FoundationApi.UI.ShowWarMenu(visible);
                    break;
                case GameMenu.Forest:
                    FoundationApi.UI.ShowForest(visible);
                    break;
                case GameMenu.Farm:
                    FoundationApi.UI.ShowFarm(visible);
                    break;
                case GameMenu.Dungeons:
                    FoundationApi.UI.ShowDungeons(visible);
                    break;
                case GameMenu.Deconstruction:
                    SetDeconstructionVisible(visible);
                    break;
                case GameMenu.Clone:
                    FoundationApi.UI.ShowClone(visible);
                    break;
                case GameMenu.AreaMove:
                    FoundationApi.UI.ShowAreaMove(visible);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(menu));
            }
        }

        public GameDeconstructionMode DeconstructionMode
        {
            get
            {
                switch (OptionsUI.DeconstructMode)
                {
                    case DeconstructMode.Hold:
                        return GameDeconstructionMode.Hold;
                    case DeconstructMode.Single:
                        return GameDeconstructionMode.Single;
                    default:
                        return GameDeconstructionMode.Toggle;
                }
            }
        }

        public bool DeconstructionHeld => Assets.Behaviour.Input.PlayerInput.Instance && Assets.Behaviour.Input.PlayerInput.Controls.UI.ToggleDeconstruct.IsPressed();

        public void SetDeconstructionVisible(bool visible) => FoundationApi.UI.ShowDeconstruction(visible);
        public bool IsPointerOverHud(Component ignoredView = null)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;
            foreach (var go in UIHelper.GetAllMouseOverUI())
            {
                if (!go || go.GetComponentInParent<WorldFlowManager>() != null)
                    continue;
                if (ignoredView && go.transform.IsChildOf(ignoredView.transform))
                    continue;
                return true;
            }

            return false;
        }

        private static DeconstructOverlay RequireOverlay(Component overlay)
        {
            if (!(overlay is DeconstructOverlay native))
                throw new ArgumentException("Expected a Foundation deconstruction hook view.", nameof(overlay));
            return native;
        }

        public void SetDeconstructionRefund(Component overlay, GameMoney amount)
        {
            var native = RequireOverlay(overlay);
            var container = RefundContainer(native);
            var display = RefundDisplay(native);
            if (!container || !display)
                return;
            if (amount != null && !amount.IsEmpty)
            {
                display.SetAmount(amount.Native);
                container.gameObject.SetActive(true);
            }
            else
                container.gameObject.SetActive(false);
        }

        public void HideDeconstructionRefund(Component overlay)
        {
            var container = RefundContainer(RequireOverlay(overlay));
            if (container)
                container.gameObject.SetActive(false);
        }

        public void PlaySound(GameUiSound sound)
        {
            switch (sound)
            {
                case GameUiSound.WindowOpen:
                    UISounds.WindowOpen();
                    break;
                case GameUiSound.WindowClose:
                    UISounds.WindowClose();
                    break;
                case GameUiSound.TurnPage:
                    UISounds.TurnPage();
                    break;
                case GameUiSound.CraftStep:
                    UISounds.CraftStep();
                    break;
                case GameUiSound.Button:
                    UISounds.Button();
                    break;
                case GameUiSound.CraftFinished:
                    UISounds.CraftFinished();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sound));
            }
        }

        public Sprite CurrencyIcon(string currencyId, int denomination = 0)
        {
            var currency = GameCurrencyMap.Resolve(currencyId);
            if (denomination < 0 || denomination > 2)
                throw new ArgumentOutOfRangeException(nameof(denomination));
            if (currency == Currency.Gold && denomination != 0 && GameUI.Instance)
            {
                var overlay = GameUI.Instance.GetComponentInChildren<DeconstructOverlay>(true);
                var display = overlay ? RefundDisplay(overlay) : null;
                if (display)
                    return (denomination == 1 ? SilverIcon(display) : CopperIcon(display)) ?? currency.GetIcon();
            }

            return currency.GetIcon();
        }

        public string FormatNumber(long value) => GameMath.FormatNumber(value);
    }
}
