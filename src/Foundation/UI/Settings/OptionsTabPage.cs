using System;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class OptionsTabPage : IDisposable
    {
        readonly IModConfigSection settings;
        readonly IModConfigTemplates templates;
        readonly RectTransform root, content;
        internal GameObject Root => root.gameObject;

        bool dirty = true;
        internal OptionsTabPage(Transform parent, IOptionsTab tab, IModConfigTemplates templates)
        {
            settings = tab.Settings;
            this.templates = templates;
            root = TechTreeTabs.Rect("CustomOptions:" + tab.ModId + ":" + tab.Id, parent);
            OptionsTabStrip.Fill(root);
            root.offsetMin = new Vector2(30, 84);
            root.offsetMax = new Vector2(-30, -OptionsTabStrip.PageTopInset);
            var viewport = TechTreeTabs.Rect("Viewport", root);
            OptionsTabStrip.Fill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            content = TechTreeTabs.Rect("Controls", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 20, 10, 10);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            settings.Changed += Changed;
            root.gameObject.SetActive(false);
        }

        void Changed()
        {
            dirty = true;
        }

        internal void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            if (visible)
                Refresh();
        }

        internal void Refresh()
        {
            if (!dirty)
                return;
            dirty = false;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var old = content.GetChild(i);
                old.gameObject.SetActive(false);
                old.SetParent(null, false);
                global::UnityEngine.Object.Destroy(old.gameObject);
            }

            var context = new ConfigContext(content, templates);
            foreach (var element in settings.Elements)
                try
                {
                    element.CreateView(context);
                }
                catch (Exception error)
                {
                    FoundationApi.Report(error);
                }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        public void Dispose()
        {
            settings.Changed -= Changed;
            if (root)
            {
                root.gameObject.SetActive(false);
                global::UnityEngine.Object.Destroy(root.gameObject);
            }
        }
    }
}
