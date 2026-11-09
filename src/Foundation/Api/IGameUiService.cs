using System;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Adventure;
using Assets.Source.Items.Data;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Native UI actions, preserving Kingdom Inc's menu ownership and styling.</summary>
    public interface IGameUiService
    {
        bool IsReady { get; }
        GameUI NativeUi { get; }

        bool IsOpen(GameUIType type);
        void ShowBuildMenu(bool visible);
        void ShowItems(bool visible);
        void ShowResearch(bool visible);
        void ShowResources(bool visible);
        void ShowDecorationMenu(bool visible);
        void ShowWarMenu(bool visible);
        void ShowForest(bool visible);
        void ShowFarm(bool visible);
        void ShowDungeons(bool visible);
        void ShowWar(WarType war);
        void ShowDungeon(DungeonType dungeon);
        void ShowCharacter(HumanItemData character, Action onClose = null);
        void ShowDeconstruction(bool visible);
        void ShowClone(bool visible);
        void ShowSearch();
        void ShowInsufficientFundsWarning();
        /// <summary>Show a warning at a specified native UI position.</summary>
        TMP_Text ShowWarning(Vector2 uiPosition, string text);
        /// <summary>Native five-second or dismissible persistent status message.</summary>
        UIStatusMessage ShowStatus(string text, string iconName, bool persistent = false, Action onClick = null);
        UIStatusMessage ShowStatus(string text, Sprite icon, bool persistent = false, Action onClick = null);
        void ToggleMap();
        void ShowAreaMove(bool visible);
        void CloseAllExcept(GameUIType? type = null);
        void TogglePopup(UIPopupMenu popup, bool visible, bool playSound = true);
    }
}
