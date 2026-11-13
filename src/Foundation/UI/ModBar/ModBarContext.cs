using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.Util;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class ModBarContext : IModBarContext, IDisposable
    {
        readonly TMP_Text font;
        readonly Image buttonStyle;
        readonly TMP_Dropdown dropdownStyle;
        readonly Toggle toggleStyle;
        readonly List<TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();
        readonly List<Action> cleanup = new List<Action>();
        bool alive = true;
        readonly bool panelTheme;
        /// <summary>B6 compatibility contract: all elements remain rooted in the
        /// game-owned UI slot, created with game templates and decorated with
        /// FoundationGameTheme. No second mod-bar renderer or new visual system.</summary>
        public RectTransform Root { get; }

        internal ModBarContext(RectTransform root, TMP_Text font, Image buttonStyle, TMP_Dropdown dropdownStyle, Toggle toggleStyle, bool panelTheme = false)
        {
            Root = root;
            this.font = font;
            this.buttonStyle = buttonStyle;
            this.dropdownStyle = dropdownStyle;
            this.toggleStyle = toggleStyle;
            this.panelTheme = panelTheme;
        }

        void RequireAlive()
        {
            if (!alive)
                throw new ObjectDisposedException(nameof(IModBarContext));
        }

        void Invoke(Action callback)
        {
            if (alive)
                try
                {
                    callback?.Invoke();
                }
                catch (Exception error)
                {
                    FoundationApi.Report(error);
                }
        }

        internal static void Fill(RectTransform rect, float x = 0, float y = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }

        public TMP_Text CreateText(string value)
        {
            RequireAlive();
            var rect = FoundationApi.NativeUiFactory.CreateRect("Text", Root);
            Fill(rect, 10, 2);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font;
            text.fontSharedMaterial = font.fontSharedMaterial;
            text.color = font.color;
            text.fontSize = panelTheme ? 18 : 22;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = panelTheme ? 13 : 16;
            text.fontSizeMax = panelTheme ? 18 : 22;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (panelTheme)
                FoundationGameTheme.ApplyText(text, 18f);
            text.text = value ?? "";
            text.raycastTarget = false;
            return text;
        }

        public Button CreateButton(string value, Action clicked)
        {
            RequireAlive();
            var rect = FoundationApi.NativeUiFactory.CreateRect("Button", Root);
            Fill(rect, 0, 2);
            var image = rect.gameObject.AddComponent<Image>();
            if (panelTheme)
                FoundationGameTheme.ApplyButton(image, buttonStyle);
            else
                CopyImage(image, buttonStyle);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            if (panelTheme)
                FoundationGameTheme.StyleButton(button);
            button.onClick.AddListener(() =>
            {
                if (alive)
                {
                    Assets.Behaviour.UI.UISounds.Button();
                    Invoke(clicked);
                }
            });
            cleanup.Add(() =>
            {
                if (button)
                    button.onClick.RemoveAllListeners();
            });
            var text = CreateText(value);
            text.transform.SetParent(rect, false);
            Fill(text.rectTransform, 10, 2);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        public TMP_Dropdown CreateDropdown(IReadOnlyList<string> options, int selectedIndex, Action<int> selected)
        {
            RequireAlive();
            if (options == null || options.Count == 0 || options.Any(o => o == null))
                throw new ArgumentException("Dropdown options are required.", nameof(options));
            if (selectedIndex < 0 || selectedIndex >= options.Count)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            if (!dropdownStyle)
                throw new InvalidOperationException("Native dropdown style is unavailable.");
            var dropdown = global::UnityEngine.Object.Instantiate(dropdownStyle, Root, false);
            dropdown.name = "Dropdown";
            dropdown.gameObject.SetActive(false);
            DisableTranslation(dropdown.gameObject);
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();
            dropdown.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            Fill((RectTransform)dropdown.transform, 0, 2);
            dropdown.template.gameObject.AddComponent<UIBlocksZoom>();
            dropdown.onValueChanged.AddListener(index => Invoke(() => selected?.Invoke(index)));
            dropdowns.Add(dropdown);
            cleanup.Add(() =>
            {
                if (dropdown)
                    dropdown.onValueChanged.RemoveAllListeners();
            });
            if (panelTheme)
                FoundationGameTheme.ApplyDropdown(dropdown);
            dropdown.gameObject.SetActive(true);
            return dropdown;
        }

        public Toggle CreateToggle(string value, bool initialValue, Action<bool> changed)
        {
            RequireAlive();
            if (!toggleStyle)
                throw new InvalidOperationException("Native toggle style is unavailable.");
            var toggle = global::UnityEngine.Object.Instantiate(toggleStyle, Root, false);
            toggle.name = "Toggle";
            toggle.gameObject.SetActive(false);
            DisableTranslation(toggle.gameObject);
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.group = null;
            toggle.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            Fill((RectTransform)toggle.transform, 0, 2);
            // Keep the *whole row* as the clickable Toggle target, but never
            // stretch the native checkbox sprite over that row. The game's
            // default Options prefab uses a fixed ~10px checkbox, so give its
            // actual graphic a properly-sized 26px native sprite slot.
            foreach (var oldLabel in toggle.GetComponentsInChildren<TMP_Text>(true))
                global::UnityEngine.Object.Destroy(oldLabel.gameObject);
            var nativeCheckbox = toggle.targetGraphic ? toggle.targetGraphic.rectTransform : null;
            var nativeCheckmark = toggle.graphic ? toggle.graphic.rectTransform : null;
            if (nativeCheckbox != null && nativeCheckbox != toggle.transform)
            {
                nativeCheckbox.anchorMin = nativeCheckbox.anchorMax = new Vector2(0f, .5f);
                nativeCheckbox.pivot = new Vector2(.5f, .5f);
                nativeCheckbox.anchoredPosition = new Vector2(29f, 0f);
                nativeCheckbox.sizeDelta = new Vector2(26f, 26f);
            }

            if (nativeCheckmark != null && nativeCheckmark != nativeCheckbox && nativeCheckmark != toggle.transform)
            {
                // If the mark is nested under the resized native checkbox,
                // center it there; otherwise align it to the same row slot.
                bool nested = nativeCheckbox != null && nativeCheckmark.IsChildOf(nativeCheckbox);
                nativeCheckmark.anchorMin = nativeCheckmark.anchorMax = new Vector2(nested ? .5f : 0f, .5f);
                nativeCheckmark.pivot = new Vector2(.5f, .5f);
                nativeCheckmark.anchoredPosition = nested ? Vector2.zero : new Vector2(29f, 0f);
                nativeCheckmark.sizeDelta = new Vector2(20f, 20f);
            }

            // The label is a sibling of the checkbox (not a child in its tiny
            // prefab rect), so it cannot cover the checkbox or clip on its art.
            var label = CreateText(value);
            label.transform.SetParent(toggle.transform, false);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(56f, 2f);
            labelRect.offsetMax = new Vector2(-12f, -2f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            // The native checkbox only raycasts over its small square. Make the
            // full slot clickable without painting over the game sprites.
            var hit = toggle.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            if (panelTheme)
                FoundationGameTheme.ApplyToggle(toggle);
            toggle.SetIsOnWithoutNotify(initialValue);
            toggle.onValueChanged.AddListener(next => Invoke(() => changed?.Invoke(next)));
            cleanup.Add(() =>
            {
                if (toggle)
                    toggle.onValueChanged.RemoveAllListeners();
            });
            toggle.gameObject.SetActive(true);
            return toggle;
        }

        public Image CreateSeparator()
        {
            RequireAlive();
            var rect = FoundationApi.NativeUiFactory.CreateRect("Separator", Root);
            rect.anchorMin = new Vector2(.5f, 0);
            rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(-1, 6);
            rect.offsetMax = new Vector2(1, -6);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = FoundationGameTheme.Brass;
            image.raycastTarget = false;
            return image;
        }

        static void DisableTranslation(GameObject control)
        {
            foreach (var translation in control.GetComponentsInChildren<Translatable>(true))
                translation.enabled = false;
        }

        internal static void CopyImage(Image target, Image source)
        {
            target.sprite = source.sprite;
            target.color = source.color;
            target.type = source.type;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }

        internal void HidePopups()
        {
            foreach (var dropdown in dropdowns)
                if (dropdown)
                    dropdown.Hide();
        }

        internal bool ClosePopups()
        {
            bool open = false;
            foreach (var dropdown in dropdowns)
                if (dropdown && dropdown.IsExpanded)
                {
                    dropdown.Hide();
                    open = true;
                }

            return open;
        }

        public void Dispose()
        {
            if (!alive)
                return;
            alive = false;
            HidePopups();
            foreach (var action in cleanup)
                action();
            cleanup.Clear();
            dropdowns.Clear();
        }
    }
}
