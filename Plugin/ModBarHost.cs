using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.Shared;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class ModBarHost : MonoBehaviour
    {
        internal const float Height = 48, Gap = 2, ReservedHeight = Height + Gap, SlideDuration = 0.22f;
        sealed class Entry
        {
            internal RectTransform Root;
            internal IModBarView View;
            internal ModBarContext Context;
        }
        readonly Dictionary<IModBarRegistration, Entry> entries = new Dictionary<IModBarRegistration, Entry>();
        RectTransform currency, parent, content, viewport;
        TMP_Text font, toggleText;
        Image buttonStyle;
        TMP_Dropdown dropdownStyle;
        Toggle toggleStyle;
        Button left, right;
        bool dirty = true;
        float previousWidth = -1;
        readonly Vector3[] currencyCorners = new Vector3[4];
        readonly List<ModBarContext> arrowContexts = new List<ModBarContext>();
        internal RectTransform Clip { get; private set; }
        internal RectTransform Bar { get; private set; }
        internal Button ToggleButton { get; private set; }
        internal bool IsExpanded { get; private set; } = true;
        internal float SlideProgress { get; private set; } = 1f;
        internal ScrollRect Scroll { get; private set; }
        internal IReadOnlyList<RectTransform> Slots => entries.Values.Select(e => e.Root).ToArray();

        internal static void Attach(GameUI ui)
        {
            var pane = ui.GetComponentInChildren<CurrencyPane>(true);
            if (!pane || pane.GetComponent<ModBarHost>()) return;
            var host = pane.gameObject.AddComponent<ModBarHost>(); host.Initialize(pane);
        }
        void Initialize(CurrencyPane pane)
        {
            currency = (RectTransform)pane.transform; parent = (RectTransform)currency.parent;
            font = currency.GetComponentInChildren<TMP_Text>(true);
            var button = GameUI.Instance.AccountantPopup.GetComponentInChildren<Button>(true);
            buttonStyle = button.GetComponent<Image>();
            dropdownStyle = GameUI.Instance.AccountantPopup.GetComponentInChildren<TMP_Dropdown>(true);
            toggleStyle = GameUI.Instance.GetComponentsInChildren<Toggle>(true).First(t => t.GetComponentInChildren<TMP_Text>(true));
            Clip = TechTreeTabs.Rect("FoundationModBarClip", parent);
            Clip.SetSiblingIndex(currency.GetSiblingIndex() + 1);
            Clip.anchorMin = new Vector2(0, 1); Clip.anchorMax = Vector2.one; Clip.pivot = new Vector2(0, 1);
            Clip.sizeDelta = new Vector2(0, Height);
            Clip.gameObject.AddComponent<RectMask2D>();
            Bar = TechTreeTabs.Rect("FoundationModBar", Clip);
            Bar.anchorMin = new Vector2(0, 1); Bar.anchorMax = Vector2.one; Bar.pivot = new Vector2(0, 1);
            Bar.sizeDelta = new Vector2(0, Height);
            ModBarContext.CopyImage(Bar.gameObject.AddComponent<Image>(), currency.GetComponent<Image>());
            Bar.gameObject.AddComponent<UIBlocksZoom>();
            viewport = TechTreeTabs.Rect("Viewport", Bar); ModBarContext.Fill(viewport, 8, 4);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            content = TechTreeTabs.Rect("Items", viewport); content.anchorMin = content.anchorMax = new Vector2(0, .5f);
            content.pivot = new Vector2(0, .5f); content.sizeDelta = new Vector2(0, 40);
            Scroll = viewport.gameObject.AddComponent<ScrollRect>(); Scroll.content = content; Scroll.viewport = viewport;
            Scroll.horizontal = true; Scroll.vertical = false; Scroll.movementType = ScrollRect.MovementType.Clamped; Scroll.scrollSensitivity = 40;
            Scroll.onValueChanged.AddListener(_ => UpdateArrows());
            left = Arrow("Previous", "<", new Vector2(0, .5f), new Vector2(8, 0), -1);
            right = Arrow("Next", ">", new Vector2(1, .5f), new Vector2(-40, 0), 1);
            BuildVisibilityToggle();
            FoundationApi.ModBar.Changed += Changed;
            Refresh();
        }
        void BuildVisibilityToggle()
        {
            var slot = TechTreeTabs.Rect("VisibilityToggle", Clip);
            slot.anchorMin = slot.anchorMax = new Vector2(.5f, 1);
            slot.pivot = new Vector2(.5f, 1);
            slot.anchoredPosition = Vector2.zero;
            slot.sizeDelta = new Vector2(42, 22);
            slot.gameObject.AddComponent<UIBlocksZoom>();
            var rect = TechTreeTabs.Rect("ChevronButton", slot);
            ModBarContext.Fill(rect, 0, 0);
            var image = rect.gameObject.AddComponent<Image>();
            ModBarContext.CopyImage(image, buttonStyle);
            ToggleButton = rect.gameObject.AddComponent<Button>();
            ToggleButton.targetGraphic = image;
            ToggleButton.navigation = new Navigation { mode = Navigation.Mode.None };
            ToggleButton.onClick.AddListener(() => { UISounds.Button(); ToggleVisibility(); });
            var labelRect = TechTreeTabs.Rect("Text", rect);
            ModBarContext.Fill(labelRect, 0, 0);
            toggleText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            toggleText.font = font.font; toggleText.fontSharedMaterial = font.fontSharedMaterial; toggleText.color = font.color;
            toggleText.fontSize = 18; toggleText.textWrappingMode = TextWrappingModes.NoWrap;
            toggleText.enableAutoSizing = false;
            toggleText.overflowMode = TextOverflowModes.Overflow;
            toggleText.extraPadding = false; toggleText.margin = Vector4.zero;
            toggleText.alignment = TextAlignmentOptions.Center; toggleText.raycastTarget = false;
            UpdateToggleText();
        }
        internal void ToggleVisibility() => SetExpanded(!IsExpanded);
        internal void SetExpanded(bool expanded)
        {
            if (IsExpanded == expanded) return;
            IsExpanded = expanded;
            if (!expanded) foreach (var entry in entries.Values) entry.Context.HidePopups();
            UpdateToggleText();
            Position();
        }
        void UpdateToggleText()
        {
            if (toggleText) toggleText.text = IsExpanded ? "^" : "v";
        }
        Button Arrow(string name, string text, Vector2 anchor, Vector2 position, int direction)
        {
            var slot = TechTreeTabs.Rect(name, Bar); slot.anchorMin = slot.anchorMax = anchor; slot.pivot = new Vector2(0, .5f);
            slot.anchoredPosition = position; slot.sizeDelta = new Vector2(32, 40);
            // Arrow contexts live for the HUD lifetime, like ordinary slots.
            var context = new ModBarContext(slot, font, buttonStyle, dropdownStyle, toggleStyle);
            var button = context.CreateButton(text, () => Scroll.horizontalNormalizedPosition = Mathf.Clamp01(Scroll.horizontalNormalizedPosition + direction * 240 / Mathf.Max(1, content.rect.width - viewport.rect.width)));
            arrowContexts.Add(context); return button;
        }
        void Changed() { dirty = true; }
        internal void StepSlide(float dt)
        {
            float target = IsExpanded ? 1f : 0f;
            if (!Mathf.Approximately(SlideProgress, target))
                SlideProgress = Mathf.MoveTowards(SlideProgress, target, Mathf.Max(0f, dt) / SlideDuration);
            Position();
        }
        void LateUpdate()
        {
            if (!Bar || !Clip) return;
            StepSlide(Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.04f));
            if (Mathf.Abs(parent.rect.width - previousWidth) > .1f) dirty = true;
            if (dirty) Refresh();
        }
        void Position()
        {
            currency.GetWorldCorners(currencyCorners);
            float bottom = parent.InverseTransformPoint(currencyCorners[0]).y;
            Clip.anchoredPosition = new Vector2(0, bottom - parent.rect.yMax - Gap);
            float eased = Mathf.SmoothStep(0f, 1f, SlideProgress);
            Bar.anchoredPosition = new Vector2(0, (1f - eased) * Height);
        }
        internal void Refresh()
        {
            if (!Bar || !Clip) return; dirty = false; Position(); previousWidth = parent.rect.width;
            var registered = FoundationApi.ModBar.Items;
            foreach (var stale in entries.Keys.Where(r => !registered.Contains(r)).ToArray()) Remove(stale);
            var leftItems = new List<(IModBarElement Element, Entry Entry)>();
            var centerItems = new List<(IModBarElement Element, Entry Entry)>();
            var rightItems = new List<(IModBarElement Element, Entry Entry)>();
            foreach (var registration in registered)
            {
                try
                {
                    var element = registration.Element;
                    ModBarRegistry.ValidateWidth(element.Width);
                    ModBarRegistry.ValidateAlignment(element.Alignment);
                    bool visible = element.IsVisible;
                    if (!entries.TryGetValue(registration, out var entry))
                    {
                        var root = TechTreeTabs.Rect(registration.ModId + ":" + registration.Id, content); root.gameObject.SetActive(false);
                        root.anchorMin = root.anchorMax = new Vector2(0, .5f); root.pivot = new Vector2(0, .5f);
                        root.sizeDelta = new Vector2(element.Width, 40);
                        var context = new ModBarContext(root, font, buttonStyle, dropdownStyle, toggleStyle);
                        entry = new Entry { Root = root, Context = context }; entries.Add(registration, entry);
                        entry.View = element.CreateView(context) ?? throw new InvalidOperationException("CreateView must return an IModBarView.");
                    }
                    entry.Root.sizeDelta = new Vector2(element.Width, 40);
                    entry.View.Refresh();
                    if (!visible) entry.Context.HidePopups();
                    entry.Root.gameObject.SetActive(visible);
                    if (visible)
                    {
                        switch (element.Alignment)
                        {
                            case ModBarAlignment.Center: centerItems.Add((element, entry)); break;
                            case ModBarAlignment.Right: rightItems.Add((element, entry)); break;
                            default: leftItems.Add((element, entry)); break;
                        }
                    }
                }
                catch (Exception error) { Remove(registration); FoundationApi.Report(error); }
            }
            float leftWidth = GroupWidth(leftItems);
            float centerWidth = GroupWidth(centerItems);
            float rightWidth = GroupWidth(rightItems);
            int nonEmptyGroups = (leftItems.Count > 0 ? 1 : 0) + (centerItems.Count > 0 ? 1 : 0) + (rightItems.Count > 0 ? 1 : 0);
            const float groupGap = 16f;
            float minRequiredWidth = leftWidth + centerWidth + rightWidth + groupGap * Mathf.Max(0, nonEmptyGroups - 1);
            float fullViewportWidth = Mathf.Max(0, parent.rect.width - 16);
            bool overflow = minRequiredWidth > fullViewportWidth;
            viewport.offsetMin = new Vector2(overflow ? 48 : 8, 4);
            viewport.offsetMax = new Vector2(overflow ? -48 : -8, -4);
            left.gameObject.SetActive(overflow); right.gameObject.SetActive(overflow);
            float availWidth = Mathf.Max(0, parent.rect.width - (overflow ? 96 : 16));
            float layoutWidth = Mathf.Max(availWidth, minRequiredWidth);

            float leftStart = 0f;
            PlaceGroup(leftItems, leftStart);
            float leftEnd = leftItems.Count > 0 ? leftStart + leftWidth : 0f;

            float rightStart = layoutWidth - rightWidth;
            float centerStart = (layoutWidth - centerWidth) * .5f;
            float chevronLeft = layoutWidth * .5f - 28f, chevronRight = layoutWidth * .5f + 28f;
            if (centerItems.Count > 0 && centerStart < chevronRight && centerStart + centerWidth > chevronLeft)
                centerStart = chevronRight;
            if (leftItems.Count > 0 && centerStart < leftEnd + groupGap) centerStart = leftEnd + groupGap;
            if (rightItems.Count > 0 && centerStart + centerWidth + groupGap > rightStart)
            {
                centerStart = Mathf.Max(leftItems.Count > 0 ? leftEnd + groupGap : 0f, rightStart - groupGap - centerWidth);
                if (centerStart + centerWidth + groupGap > rightStart) rightStart = centerStart + centerWidth + groupGap;
            }
            if (centerItems.Count == 0 && leftItems.Count > 0 && rightItems.Count > 0 && rightStart < leftEnd + groupGap)
                rightStart = leftEnd + groupGap;

            PlaceGroup(centerItems, centerStart);
            PlaceGroup(rightItems, rightStart);

            float furthest = rightItems.Count > 0 ? rightStart + rightWidth : (centerItems.Count > 0 ? centerStart + centerWidth : leftEnd);
            content.sizeDelta = new Vector2(overflow ? furthest : availWidth, 40);
            Canvas.ForceUpdateCanvases();
            if (!overflow) content.anchoredPosition = Vector2.zero;
            else content.anchoredPosition = new Vector2(Mathf.Clamp(content.anchoredPosition.x, Mathf.Min(0, viewport.rect.width - content.rect.width), 0), 0);
            UpdateArrows();
        }
        static float GroupWidth(List<(IModBarElement Element, Entry Entry)> group)
            => group.Count == 0 ? 0f : group.Sum(g => g.Element.Width) + 8f * (group.Count - 1);
        static void PlaceGroup(List<(IModBarElement Element, Entry Entry)> group, float startX)
        {
            float x = startX;
            for (int i = 0; i < group.Count; i++)
            {
                group[i].Entry.Root.anchoredPosition = new Vector2(x, 0);
                x += group[i].Element.Width + 8f;
            }
        }
        void UpdateArrows()
        { if (left) left.interactable = Scroll.horizontalNormalizedPosition > .001f; if (right) right.interactable = Scroll.horizontalNormalizedPosition < .999f; }
        void Remove(IModBarRegistration registration)
        {
            if (!entries.TryGetValue(registration, out var entry)) return;
            entries.Remove(registration);
            try { entry.View?.Dispose(); } catch (Exception error) { FoundationApi.Report(error); }
            entry.Context.Dispose();
            if (entry.Root) { entry.Root.gameObject.SetActive(false); Destroy(entry.Root.gameObject); }
        }
        void OnEnable()
        {
            if (Clip) Clip.gameObject.SetActive(true);
            if (Bar) { Bar.gameObject.SetActive(true); dirty = true; }
        }
        void OnDisable()
        {
            foreach (var entry in entries.Values) entry.Context.HidePopups();
            if (Bar) Bar.gameObject.SetActive(false);
            if (Clip) Clip.gameObject.SetActive(false);
        }
        internal bool ClosePopups()
        {
            bool open = false;
            foreach (var entry in entries.Values) open = entry.Context.ClosePopups() || open;
            return open;
        }
        void OnDestroy()
        {
            FoundationApi.ModBar.Changed -= Changed;
            foreach (var key in entries.Keys.ToArray()) Remove(key);
            foreach (var context in arrowContexts) context.Dispose();
            if (Clip) Destroy(Clip.gameObject);
            else if (Bar) Destroy(Bar.gameObject);
        }
    }
}
