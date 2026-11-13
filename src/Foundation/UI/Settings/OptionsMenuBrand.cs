using System;
using System.Linq;
using Assets.Behaviour.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class OptionsMenuBrand : IDisposable
    {
        readonly Image[] originals = Array.Empty<Image>();
        readonly bool[] enabled = Array.Empty<bool>();
        readonly RectTransform decoration;
        bool shown;
        internal OptionsMenuBrand(OptionsUI options)
        {
            var menu = options.GetComponentInParent<MainMenuUI>();
            if (!menu)
                return;
            originals = menu.GetComponentsInChildren<Image>(true).Where(i => i.sprite && i.sprite.name == "KingdomIncLogo_3x").ToArray();
            if (originals.Length == 0)
                return;
            enabled = originals.Select(i => i.enabled).ToArray();
            decoration = TechTreeTabs.Rect("OptionsLogo", options.transform);
            decoration.SetAsFirstSibling();
            decoration.anchorMin = decoration.anchorMax = new Vector2(.5f, 1);
            decoration.pivot = new Vector2(.5f, 0);
            decoration.anchoredPosition = new Vector2(0, -4);
            var sprite = originals[0].sprite;
            decoration.sizeDelta = new Vector2(380, 380 * sprite.rect.height / sprite.rect.width);
            var image = decoration.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            decoration.gameObject.SetActive(false);
        }

        internal void Show()
        {
            if (!decoration)
                return;
            shown = true;
            foreach (var image in originals)
                if (image)
                    image.enabled = false;
            decoration.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            if (!shown)
                return;
            shown = false;
            for (int i = 0; i < originals.Length; i++)
                if (originals[i])
                    originals[i].enabled = enabled[i];
            if (decoration)
                decoration.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            Hide();
            if (decoration)
                global::UnityEngine.Object.Destroy(decoration.gameObject);
        }
    }
}
