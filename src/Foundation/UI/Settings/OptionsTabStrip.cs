using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class OptionsTabStrip : IDisposable
    {
        internal const float Height = 44;
        internal const float TopInset = 52;
        internal const float PageTopInset = TopInset + Height + 20;
        readonly RectTransform root, leftViewport, leftContent, rightContent;
        readonly TMP_Text font;
        readonly Sprite tabSprite;
        readonly ScrollRect scroll;
        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        string selected = "game";
        float lastWidth;
        internal RectTransform Root => root;
        internal IReadOnlyDictionary<string, Button> Buttons => buttons;
        internal OptionsTabStrip(Transform parent, TMP_Text font)
        {
            this.font = font;
            var source = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "Button02");
            if (!source) throw new InvalidOperationException("Native bronze tab frame unavailable.");
            // The top half of the native bronze frame makes the mockup's angled
            // shoulders and straight sides, without the pointed bottom of a button.
            var area = source.rect; area.y += area.height / 2; area.height /= 2;
            tabSprite = Sprite.Create(source.texture, area, new Vector2(.5f, 0), source.pixelsPerUnit, 0, SpriteMeshType.FullRect,
                new Vector4(source.border.x, 0, source.border.z, Mathf.Min(source.border.w, area.height - 1)));
            root = TechTreeTabs.Rect("OptionsTabs", parent);
            root.anchorMin = new Vector2(0, 1); root.anchorMax = Vector2.one; root.pivot = new Vector2(.5f, 1);
            root.anchoredPosition = new Vector2(0, -TopInset); root.sizeDelta = new Vector2(0, Height);
            leftViewport = TechTreeTabs.Rect("LeftTabsViewport", root); Fill(leftViewport);
            leftViewport.gameObject.AddComponent<RectMask2D>();
            leftViewport.gameObject.AddComponent<Image>().color = Color.clear;
            leftContent = TechTreeTabs.Rect("LeftTabs", leftViewport);
            leftContent.anchorMin = leftContent.anchorMax = Vector2.zero; leftContent.pivot = Vector2.zero;
            leftContent.sizeDelta = new Vector2(0, Height);
            scroll = leftViewport.gameObject.AddComponent<ScrollRect>(); scroll.content = leftContent; scroll.viewport = leftViewport;
            scroll.horizontal = true; scroll.vertical = false; scroll.scrollSensitivity = 40; scroll.movementType = ScrollRect.MovementType.Clamped;
            rightContent = TechTreeTabs.Rect("RightTabs", root); rightContent.anchorMin = rightContent.anchorMax = new Vector2(1, 0);
            rightContent.pivot = new Vector2(1, 0); rightContent.anchoredPosition = new Vector2(-30, 0);
            var line = TechTreeTabs.Rect("Divider", root); line.anchorMin = Vector2.zero; line.anchorMax = new Vector2(1, 0);
            line.offsetMin = new Vector2(28, -1); line.offsetMax = new Vector2(-28, 1);
            var image = line.gameObject.AddComponent<Image>(); image.color = new Color(.55f, .43f, .2f); image.raycastTarget = false;
        }
        internal void Rebuild(IReadOnlyList<IOptionsTabRegistration> tabs, Action<string> selectBuiltin, Action<IOptionsTabRegistration> selectCustom)
        {
            foreach (var button in buttons.Values) { button.gameObject.SetActive(false); UnityEngine.Object.Destroy(button.gameObject); }
            buttons.Clear(); float x = 0;
            x += Make("game", "Game", leftContent, x, () => selectBuiltin("game"));
            foreach (var item in tabs) { var captured = item; x += Make(Key(item), item.Tab.Title, leftContent, x, () => selectCustom(captured)); }
            leftContent.sizeDelta = new Vector2(x, Height);
            float rightWidth = Make("mods", "Mods", rightContent, 0, () => selectBuiltin("mods"));
            rightWidth += Make("installed", "Installed", rightContent, rightWidth, () => selectBuiltin("installed"));
            rightContent.sizeDelta = new Vector2(rightWidth, Height); Resize(); Select(selected);
        }
        float Make(string key, string title, RectTransform parent, float x, Action clicked)
        {
            var rect = TechTreeTabs.Rect("Tab:" + key, parent); rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, 0);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = tabSprite; image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { Assets.Behaviour.UI.UISounds.TurnPage(); clicked(); });
            var label = TechTreeTabs.Rect("Label", rect); Fill(label); label.offsetMin = new Vector2(24, 0); label.offsetMax = new Vector2(-24, 0);
            var text = label.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font.font; text.fontSharedMaterial = font.fontSharedMaterial;
            text.fontSize = 24; text.color = Color.white; text.text = title; text.alignment = TextAlignmentOptions.Midline;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
            float width = Mathf.Ceil(text.GetPreferredValues(title).x) + 56;
            rect.sizeDelta = new Vector2(width, Height);
            text.ForceMeshUpdate(true);
            // Pixel-font ascenders do not sit symmetrically in the line box.
            // Center the glyph bounds, then lower the label for the tab's visual baseline.
            var correction = label.rect.center - (Vector2)text.textBounds.center;
            label.anchoredPosition += correction + new Vector2(0, -4);
            buttons.Add(key, button); return width - 4;
        }
        internal static string Key(IOptionsTabRegistration tab) => "custom:" + tab.Tab.ModId + ":" + tab.Tab.Id;
        internal void Select(string key)
        {
            selected = key;
            foreach (var pair in buttons)
            {
                bool active = pair.Key == key;
                pair.Value.GetComponent<Image>().color = active ? Color.white : new Color(.77f, .73f, .65f);
                pair.Value.GetComponentInChildren<TMP_Text>().color = active ? Color.white : new Color(.9f, .9f, .9f);
            }
            if (buttons.TryGetValue(key, out var target) && target.transform.parent == leftContent)
            {
                var rect = (RectTransform)target.transform; float pos = leftContent.anchoredPosition.x;
                if (rect.anchoredPosition.x + pos < 0) pos = -rect.anchoredPosition.x;
                if (rect.anchoredPosition.x + rect.rect.width + pos > leftViewport.rect.width) pos = leftViewport.rect.width - rect.anchoredPosition.x - rect.rect.width;
                leftContent.anchoredPosition = new Vector2(Mathf.Clamp(pos, Mathf.Min(0, leftViewport.rect.width - leftContent.rect.width), 0), 0);
            }
        }
        internal void Resize()
        {
            lastWidth = root.rect.width;
            leftViewport.offsetMin = new Vector2(30, 0);
            leftViewport.offsetMax = new Vector2(-30 - rightContent.rect.width - 64, 0);
        }
        internal void Update() { if (Mathf.Abs(lastWidth - root.rect.width) > .1f) { Resize(); Select(selected); } }
        internal static void Fill(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        public void Dispose() { if (root) UnityEngine.Object.Destroy(root.gameObject); if (tabSprite) UnityEngine.Object.Destroy(tabSprite); }
    }
}
