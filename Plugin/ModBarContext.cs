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
        public RectTransform Root { get; }
        internal ModBarContext(RectTransform root, TMP_Text font, Image buttonStyle, TMP_Dropdown dropdownStyle, Toggle toggleStyle)
        { Root = root; this.font = font; this.buttonStyle = buttonStyle; this.dropdownStyle = dropdownStyle; this.toggleStyle = toggleStyle; }
        void RequireAlive() { if (!alive) throw new ObjectDisposedException(nameof(IModBarContext)); }
        void Invoke(Action callback) { if (alive) try { callback?.Invoke(); } catch (Exception error) { FoundationApi.Report(error); } }
        internal static void Fill(RectTransform rect, float x = 0, float y = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y); }
        public TMP_Text CreateText(string value)
        {
            RequireAlive();
            var rect = TechTreeTabs.Rect("Text", Root); Fill(rect, 10, 2);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font; text.fontSharedMaterial = font.fontSharedMaterial; text.color = font.color;
            text.fontSize = 22; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 22;
            text.overflowMode = TextOverflowModes.Ellipsis; text.alignment = TextAlignmentOptions.MidlineLeft;
            text.text = value ?? ""; text.raycastTarget = false; return text;
        }
        public Button CreateButton(string value, Action clicked)
        {
            RequireAlive();
            var rect = TechTreeTabs.Rect("Button", Root); Fill(rect, 0, 2);
            var image = rect.gameObject.AddComponent<Image>(); CopyImage(image, buttonStyle);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { if (alive) { Assets.Behaviour.UI.UISounds.Button(); Invoke(clicked); } });
            cleanup.Add(() => { if (button) button.onClick.RemoveAllListeners(); });
            var text = CreateText(value); text.transform.SetParent(rect, false); Fill(text.rectTransform, 10, 2); text.alignment = TextAlignmentOptions.Center;
            return button;
        }
        public TMP_Dropdown CreateDropdown(IReadOnlyList<string> options, int selectedIndex, Action<int> selected)
        {
            RequireAlive();
            if (options == null || options.Count == 0 || options.Any(o => o == null)) throw new ArgumentException("Dropdown options are required.", nameof(options));
            if (selectedIndex < 0 || selectedIndex >= options.Count) throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            if (!dropdownStyle) throw new InvalidOperationException("Native dropdown style is unavailable.");
            var dropdown = UnityEngine.Object.Instantiate(dropdownStyle, Root, false); dropdown.name = "Dropdown";
            dropdown.gameObject.SetActive(false); DisableTranslation(dropdown.gameObject);
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent(); dropdown.ClearOptions(); dropdown.AddOptions(options.ToList());
            dropdown.SetValueWithoutNotify(selectedIndex); dropdown.RefreshShownValue();
            dropdown.navigation = new Navigation { mode = Navigation.Mode.None };
            Fill((RectTransform)dropdown.transform, 0, 2);
            dropdown.template.gameObject.AddComponent<UIBlocksZoom>();
            dropdown.onValueChanged.AddListener(index => Invoke(() => selected?.Invoke(index)));
            dropdowns.Add(dropdown); cleanup.Add(() => { if (dropdown) dropdown.onValueChanged.RemoveAllListeners(); });
            dropdown.gameObject.SetActive(true); return dropdown;
        }
        public Toggle CreateToggle(string value, bool initialValue, Action<bool> changed)
        {
            RequireAlive();
            if (!toggleStyle) throw new InvalidOperationException("Native toggle style is unavailable.");
            var toggle = UnityEngine.Object.Instantiate(toggleStyle, Root, false); toggle.name = "Toggle";
            toggle.gameObject.SetActive(false); DisableTranslation(toggle.gameObject);
            toggle.onValueChanged = new Toggle.ToggleEvent(); toggle.group = null;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            Fill((RectTransform)toggle.transform, 0, 2);
            // The native checkbox only raycasts over its small square. Make the whole
            // bar slot clickable, including the non-raycasting label.
            var hit = toggle.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var label = toggle.GetComponentInChildren<TMP_Text>(true);
            if (!label) { label = CreateText(value); label.transform.SetParent(toggle.transform, false); Fill(label.rectTransform, 34, 2); }
            label.text = value ?? ""; label.raycastTarget = false;
            toggle.SetIsOnWithoutNotify(initialValue); toggle.onValueChanged.AddListener(next => Invoke(() => changed?.Invoke(next)));
            cleanup.Add(() => { if (toggle) toggle.onValueChanged.RemoveAllListeners(); });
            toggle.gameObject.SetActive(true); return toggle;
        }
        public Image CreateSeparator()
        {
            RequireAlive();
            var rect = TechTreeTabs.Rect("Separator", Root);
            rect.anchorMin = new Vector2(.5f, 0);
            rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(-1, 6);
            rect.offsetMax = new Vector2(1, -6);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.65f, .52f, .34f, .75f);
            image.raycastTarget = false;
            return image;
        }
        static void DisableTranslation(GameObject control)
        { foreach (var translation in control.GetComponentsInChildren<Translatable>(true)) translation.enabled = false; }
        internal static void CopyImage(Image target, Image source)
        {
            target.sprite = source.sprite; target.color = source.color; target.type = source.type;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }
        internal void HidePopups() { foreach (var dropdown in dropdowns) if (dropdown) dropdown.Hide(); }
        internal bool ClosePopups()
        {
            bool open = false;
            foreach (var dropdown in dropdowns) if (dropdown && dropdown.IsExpanded) { dropdown.Hide(); open = true; }
            return open;
        }
        public void Dispose()
        {
            if (!alive) return; alive = false; HidePopups();
            foreach (var action in cleanup) action(); cleanup.Clear(); dropdowns.Clear();
        }
    }
}
