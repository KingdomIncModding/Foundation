using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.Shared;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class ModResearchSidebar : MonoBehaviour
    {
        internal RectTransform Panel;
        internal ScrollRect Scroll;
        internal readonly Dictionary<string, Button> Buttons = new Dictionary<string, Button>();
        TechTreeTabs tabs;
        RectTransform content;
        TMP_Text font, empty;
        Image rowSource;
        Sprite selected;
        string signature;
        internal void Initialize(TechTreeTabs tabs, RectTransform ui, TMP_Text font, Image rowSource, Sprite selected, Image frame)
        {
            this.tabs = tabs; this.font = font; this.rowSource = rowSource; this.selected = selected;
            Panel = TechTreeTabs.Rect("FoundationModList", ui);
            Panel.anchorMin = new Vector2(0, 0); Panel.anchorMax = new Vector2(0, 1);
            Panel.pivot = new Vector2(0, 1); Panel.offsetMin = new Vector2(20, 120); Panel.offsetMax = new Vector2(292, -188);
            var background = Panel.gameObject.AddComponent<Image>();
            Panel.gameObject.AddComponent<UIBlocksZoom>();
            background.sprite = frame.sprite; background.type = Image.Type.Sliced; background.pixelsPerUnitMultiplier = frame.pixelsPerUnitMultiplier;
            var title = TechTreeTabs.Rect("Title", Panel);
            title.anchorMin = title.anchorMax = new Vector2(.5f, 1); title.pivot = new Vector2(.5f, 1);
            title.sizeDelta = new Vector2(248, 44); title.anchoredPosition = new Vector2(0, -12);
            Label(title, "Mod List", 24);
            var viewport = TechTreeTabs.Rect("Viewport", Panel);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12, 16); viewport.offsetMax = new Vector2(-12, -64);
            var input = viewport.gameObject.AddComponent<Image>(); input.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = TechTreeTabs.Rect("Mods", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            Scroll = viewport.gameObject.AddComponent<ScrollRect>(); Scroll.viewport = viewport; Scroll.content = content;
            Scroll.horizontal = false; Scroll.vertical = true; Scroll.movementType = ScrollRect.MovementType.Clamped; Scroll.scrollSensitivity = 30;
            var emptyRect = TechTreeTabs.Rect("Empty", viewport);
            emptyRect.anchorMin = emptyRect.anchorMax = new Vector2(.5f, 1); emptyRect.pivot = new Vector2(.5f, 1);
            emptyRect.anchoredPosition = new Vector2(0, -12); emptyRect.sizeDelta = new Vector2(230, 80);
            empty = Label(emptyRect, "No mods registered", 20);
            Refresh();
        }
        TMP_Text Label(RectTransform parent, string value, float size)
        {
            var rect = TechTreeTabs.Rect("Label", parent); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 4); rect.offsetMax = new Vector2(-10, -4);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font; text.fontSharedMaterial = font.fontSharedMaterial; text.color = font.color;
            text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Center; text.text = value; text.raycastTarget = false;
            return text;
        }
        internal void Refresh()
        {
            if (!content) return;
            var mods = FoundationApi.TechMods;
            string next = string.Join("\n", mods.Select(m => m.Id + "\t" + m.Name));
            if (next != signature)
            {
                signature = next;
                foreach (var button in Buttons.Values) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
                Buttons.Clear();
                for (int i = 0; i < mods.Count; i++)
                {
                    var mod = mods[i];
                    var row = TechTreeTabs.Rect("Mod:" + mod.Id, content);
                    row.anchorMin = new Vector2(0, 1); row.anchorMax = Vector2.one; row.pivot = new Vector2(.5f, 1);
                    row.sizeDelta = new Vector2(0, 56); row.anchoredPosition = new Vector2(0, -i * 64);
                    var image = row.gameObject.AddComponent<Image>(); image.sprite = rowSource.sprite; image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = rowSource.pixelsPerUnitMultiplier;
                    var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                    var label = Label(row, mod.Name, 22);
                    label.rectTransform.offsetMin = new Vector2(28, 4); label.rectTransform.offsetMax = new Vector2(-28, -4);
                    label.fontSizeMin = 14;
                    button.onClick.AddListener(() => tabs.SelectMod(mod.Id)); Buttons.Add(mod.Id, button);
                }
                content.sizeDelta = new Vector2(0, mods.Count * 64); Scroll.verticalNormalizedPosition = 1;
            }
            empty.gameObject.SetActive(mods.Count == 0);
            foreach (var pair in Buttons) pair.Value.GetComponent<Image>().sprite = pair.Key == tabs.SelectedModId ? selected : rowSource.sprite;
        }
        void OnDestroy() { if (Panel) Destroy(Panel.gameObject); }
    }
}
