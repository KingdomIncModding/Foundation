using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    /// <summary>
    /// Kingdom Inc's existing UI art is the authoritative theme. The names below
    /// were confirmed against the extracted game asset metadata (not invented
    /// sprites). The game itself owns and loads every sprite; Foundation never
    /// imports or redistributes extracted textures.
    /// </summary>
    internal static class FoundationGameTheme
    {
        // Pixel references were sampled from the game's native Options UI.
        // These are used only for solid backing, contrast and missing-sprite
        // fallbacks; game sprites supply the visible border/button decorations.
        internal static readonly Color Panel = new Color(38f / 255f, 26f / 255f, 35f / 255f, 1f);
        internal static readonly Color Well = new Color(26f / 255f, 18f / 255f, 26f / 255f, 1f);
        internal static readonly Color Ink = new Color(234f / 255f, 223f / 255f, 203f / 255f, 1f);
        internal static readonly Color SecondaryInk = new Color(205f / 255f, 190f / 255f, 161f / 255f, 1f);
        internal static readonly Color Brass = new Color(174f / 255f, 130f / 255f, 73f / 255f, 1f);
        internal static readonly Color DisabledInk = new Color(156f / 255f, 143f / 255f, 128f / 255f, 1f);

        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static bool indexed;
        private static Sprite optionsButton;
        private static Sprite optionsSliderHandle;
        private static ColorBlock optionsButtonColors;
        private static bool optionsButtonColorsAvailable;

        // Rebind on every new GameUI scene. Unity can unload scene-owned
        // images across world/menu transitions; do not keep dead references.
        internal static void InvalidateRuntimeReferences()
        {
            indexed = false;
            sprites.Clear();
            optionsButton = null;
            optionsSliderHandle = null;
            optionsButtonColorsAvailable = false;
        }

        private static void IndexNativeAssets()
        {
            if (indexed) return;
            indexed = true;
            // Once per scene process; do not scan all game assets per widget.
            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (!sprite) continue;
                if (!sprites.ContainsKey(sprite.name)) sprites.Add(sprite.name, sprite);
            }

            // Options is the strongest visual reference (beveled amber buttons).
            // Prefer the game's actual Button template over a guessed tint.
            foreach (var options in Resources.FindObjectsOfTypeAll<OptionsUI>())
            {
                if (!options) continue;
                var button = options.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b && (b.name == "Apply" || b.name == "Cancel" || b.name == "KeyBindings"));
                var image = button ? button.GetComponent<Image>() : null;
                if (!optionsButton && image && image.sprite) optionsButton = image.sprite;
                if (!optionsButtonColorsAvailable && button)
                {
                    optionsButtonColors = button.colors;
                    optionsButtonColorsAvailable = true;
                }
                var slider = options.GetComponentInChildren<Slider>(true);
                var thumb = slider && slider.handleRect ? slider.handleRect.GetComponent<Image>() : null;
                if (!optionsSliderHandle && thumb && thumb.sprite) optionsSliderHandle = thumb.sprite;
                if (optionsButton && optionsSliderHandle) break;
            }
        }

        private static Sprite Sprite(string name)
        {
            IndexNativeAssets();
            return sprites.TryGetValue(name, out var found) && found ? found : null;
        }

        private static void UseSlice(Image graphic, Sprite sprite, Color tint)
        {
            graphic.sprite = sprite;
            graphic.type = Image.Type.Sliced;
            graphic.color = tint;
            graphic.preserveAspect = false;
        }

        internal static void ApplyPanel(Image image)
        {
            // Always opaque even when source native popup has transparent art.
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Panel;
            image.raycastTarget = true;
        }

        /// <summary>Add a native 9-sliced border without covering child controls.</summary>
        internal static bool AddPanelBorder(RectTransform panel)
        {
            var sprite = Sprite("DecoRectBorder") ?? Sprite("DefaultBorderDark");
            if (!sprite) return false;
            var frame = TechTreeTabs.Rect("NativeGameFrame", panel);
            ModBarContext.Fill(frame);
            var image = frame.gameObject.AddComponent<Image>();
            UseSlice(image, sprite, Color.white);
            image.raycastTarget = false;
            return true;
        }

        internal static void ApplyButton(Image image, Image nativeFallback = null)
        {
            if (nativeFallback) ModBarContext.CopyImage(image, nativeFallback);
            IndexNativeAssets();
            var sprite = optionsButton ?? Sprite("Button02") ?? Sprite("Button03");
            if (sprite) UseSlice(image, sprite, Color.white);
            else
            {
                // Avoid the original fluorescent-green native machine button
                // when no decorative Options sprite is loaded.
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = Well;
                var edge = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
                edge.effectColor = Brass;
                edge.effectDistance = new Vector2(1, -1);
            }
        }

        internal static void StyleButton(Button button)
        {
            if (!button) return;
            IndexNativeAssets();
            // Never require an Animator on dynamically created buttons.
            // Preserve the actual game's hover/pressed/disabled color state.
            button.transition = Selectable.Transition.ColorTint;
            if (optionsButtonColorsAvailable) button.colors = optionsButtonColors;
            else
            {
                var state = button.colors;
                state.normalColor = Color.white;
                state.highlightedColor = new Color(1f, .93f, .80f, 1f);
                state.pressedColor = new Color(.80f, .67f, .46f, 1f);
                state.selectedColor = state.highlightedColor;
                state.disabledColor = new Color(.55f, .52f, .48f, .7f);
                button.colors = state;
            }
        }

        internal static void ApplySliderHandle(Image image, Image nativeFallback)
        {
            // Prefer the exact slider thumb artwork already in native Options.
            IndexNativeAssets();
            var handleSprite = optionsSliderHandle ?? Sprite("ScrollHandle");
            if (handleSprite)
            {
                UseSlice(image, handleSprite, Color.white);
                image.raycastTarget = true;
            }
            else ApplyButton(image, nativeFallback);
        }

        internal static void FrameColorSwatch(RectTransform rect)
        {
            // A swatch must display its *actual* color. A sliced native sprite
            // might have an opaque centre that would conceal the selected hue;
            // use a slim native-brass outline instead of overlaying its pixels.
            var edge = rect.gameObject.AddComponent<Outline>();
            edge.effectColor = Brass;
            edge.effectDistance = new Vector2(1, -1);
        }

        internal static void ApplyWell(Image image)
        {
            var sprite = Sprite("DefaultBorderDark") ?? Sprite("SimpleBox") ?? Sprite("SelectBox");
            // A solid backing is required for readable text regardless of sprite
            // centre alpha. The decorative border is added as a child overlay.
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Well;
            image.raycastTarget = true;
            if (!sprite) return;
            var overlay = TechTreeTabs.Rect("NativeWellBorder", image.transform);
            ModBarContext.Fill(overlay);
            overlay.SetAsFirstSibling();
            var border = overlay.gameObject.AddComponent<Image>();
            UseSlice(border, sprite, Color.white);
            border.raycastTarget = false;
        }

        internal static void ApplyText(TMP_Text text, float max = 18f)
        {
            if (!text) return;
            text.color = Ink;
            text.fontSize = max;
            text.fontSizeMin = Mathf.Min(13f, max);
            text.fontSizeMax = max;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        internal static void ApplyDropdown(TMP_Dropdown dropdown)
        {
            if (!dropdown) return;
            var background = dropdown.GetComponent<Image>();
            if (background) ApplyWell(background);
            ApplyText(dropdown.captionText, 17f);
            ApplyText(dropdown.itemText, 16f);
            if (dropdown.template)
            {
                var templateBackground = dropdown.template.GetComponent<Image>();
                if (templateBackground) ApplyWell(templateBackground);
                var itemBackground = dropdown.itemText ? dropdown.itemText.GetComponentInParent<Toggle>() : null;
                if (itemBackground && itemBackground.targetGraphic is Image selectedGraphic)
                {
                    selectedGraphic.color = new Color(.27f, .19f, .23f, 1f);
                }
            }
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            if (!toggle) return;
            var text = toggle.GetComponentInChildren<TMP_Text>(true);
            ApplyText(text, 17f);
            // Preserve native Toggle graphics and its checkmark. Avoid replacing
            // a game's checkbox with a new, incompatible colored block.
        }

        /// <summary>WCAG contrast ratio; useful for automated palette regression checks.</summary>
        internal static float Contrast(Color a, Color b)
        {
            float aa = Luminance(a), bb = Luminance(b);
            return (Mathf.Max(aa, bb) + .05f) / (Mathf.Min(aa, bb) + .05f);
        }

        private static float Luminance(Color c)
        {
            return .2126f * Channel(c.r) + .7152f * Channel(c.g) + .0722f * Channel(c.b);
        }
        private static float Channel(float c)
        {
            return c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
        }
    }
}
