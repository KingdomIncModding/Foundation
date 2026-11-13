using System;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Adventure;
using Assets.Source.Items.Data;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameUiService : IGameUiService
    {
        static GameUI Ui => GameUI.Instance ? GameUI.Instance : throw new InvalidOperationException("Native GameUI is not active.");
        public bool IsReady => GameUI.Instance;
        public GameUI NativeUi => GameUI.Instance;
        public bool IsOpen(GameUIType type) => Ui.IsActive(type);
        public void ShowBuildMenu(bool visible) => Ui.ToggleBuyMenu(visible);
        public void ShowItems(bool visible) => Ui.ToggleItemMenu(visible);
        public void ShowResearch(bool visible) => Ui.ToggleTechMenu(visible);
        public void ShowResources(bool visible) => Ui.ToggleResourceMenu(visible);
        public void ShowDecorationMenu(bool visible) => Ui.ToggleDecoMenu(visible);
        public void ShowWarMenu(bool visible) => Ui.ToggleWarEffortMenu(visible);
        public void ShowForest(bool visible) => Ui.ToggleForestCrewMenu(visible);
        public void ShowFarm(bool visible) => Ui.ToggleFarmMenu(visible);
        public void ShowDungeons(bool visible) => Ui.ToggleDungeonMenu(visible);
        public void ShowWar(WarType war) => Ui.ToggleWarEffortMenu(war ?? throw new ArgumentNullException(nameof(war)));
        public void ShowDungeon(DungeonType dungeon) => Ui.ToggleDungeonMenu(dungeon ?? throw new ArgumentNullException(nameof(dungeon)));
        public void ShowCharacter(HumanItemData character, Action onClose = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            Ui.ShowCharacterDetails(character, onClose);
        }
        public void ShowDeconstruction(bool visible) => Ui.ToggleDeconstructMode(visible);
        public void ShowClone(bool visible) => Ui.ToggleCloneMode(visible);
        public void ShowSearch() => Ui.ToggleSearch();
        public void ShowInsufficientFundsWarning() => Ui.InsufficientFundsWarning();
        public TMP_Text ShowWarning(Vector2 uiPosition, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Warning text is required.", nameof(text));
            return Ui.ShowWarning(uiPosition, text);
        }
        public UIStatusMessage ShowStatus(string text, string iconName, bool persistent = false, Action onClick = null)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Status text is required.", nameof(text));
            if (string.IsNullOrWhiteSpace(iconName)) throw new ArgumentException("Native icon name is required.", nameof(iconName));
            _ = Ui; // Native status message parent is initialized in GameUI.Awake.
            return UIStatusMessage.Show(text, iconName, persistent, Guard(onClick));
        }
        public UIStatusMessage ShowStatus(string text, Sprite icon, bool persistent = false, Action onClick = null)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Status text is required.", nameof(text));
            if (!icon) throw new ArgumentNullException(nameof(icon));
            _ = Ui;
            return UIStatusMessage.Show(text, icon, persistent, Guard(onClick));
        }
        static Action Guard(Action handler) => handler == null ? null : new Action(() =>
        {
            try { handler(); }
            catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Native status callback failed.", error)); }
        });
        public void ToggleMap() => Ui.ToggleMap();
        public void ShowAreaMove(bool visible) => Ui.ToggleAreaMove(visible);
        public void CloseAllExcept(GameUIType? type = null) => Ui.CloseAllExcept(type);
        public void TogglePopup(UIPopupMenu popup, bool visible, bool playSound = true)
        {
            if (!popup) throw new ArgumentNullException(nameof(popup));
            Ui.TogglePopupMenu(popup, visible, playSound);
        }
    }
}
