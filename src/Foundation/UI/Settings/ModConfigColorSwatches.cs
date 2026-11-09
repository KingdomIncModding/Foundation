using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Native-settings color selector rendered as directly clickable rectangular swatches.
    /// No wheel or dropdown. Changes participate in the standard Apply/Cancel edit session.
    /// </summary>
    public sealed class ModConfigColorSwatches : IModConfigColorSwatches, IModConfigEditSession
    {
        readonly IReadOnlyList<Color> palette;
        readonly Func<Color> getter;
        readonly List<Outline> outlines = new List<Outline>();
        Color committed;
        Color pending;
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public IReadOnlyList<Color> Palette => palette;
        public Action<Color> OnValueChanged { get; set; }
        public Color Value
        {
            get => pending;
            set { pending = Select(value); UpdateSelection(); }
        }
        public ModConfigColorSwatches(string label, IReadOnlyList<Color> colors, Color initial,
            Action<Color> onChanged, Func<Color> getCurrent = null)
        {
            if (colors == null || colors.Count == 0) throw new ArgumentException("Provide at least one color swatch.", nameof(colors));
            if (colors.Count > 24) throw new ArgumentException("Use at most 24 colors per swatch row.", nameof(colors));
            palette = Array.AsReadOnly(colors.ToArray());
            Label = label ?? "Color";
            getter = getCurrent;
            OnValueChanged = onChanged;
            committed = Select(initial);
            pending = committed;
            Id = Guid.NewGuid().ToString("N");
        }
        Color Select(Color candidate)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < palette.Count; i++)
            {
                var c = palette[i];
                float distance = (c.r - candidate.r) * (c.r - candidate.r) +
                    (c.g - candidate.g) * (c.g - candidate.g) +
                    (c.b - candidate.b) * (c.b - candidate.b) +
                    (c.a - candidate.a) * (c.a - candidate.a);
                if (distance < best) { best = distance; nearest = i; }
            }
            return palette[nearest];
        }
        void UpdateSelection()
        {
            for (int i = 0; i < outlines.Count; i++)
                if (outlines[i]) outlines[i].effectColor = palette[i] == pending
                    ? new Color(1f, 0.81f, 0.30f, 1f) : new Color(0.17f, 0.14f, 0.11f, 0.8f);
        }
        public GameObject CreateView(IModConfigContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            outlines.Clear();
            var root = context.Templates.CreateEmptyRow(context.Container, 95);
            root.name = "FoundationColorSwatches";
            root.SetActive(IsVisible);
            var stack = root.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 4;
            stack.padding = new RectOffset(4, 4, 0, 0);
            stack.childControlWidth = true;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            TMP_Text labelText;
            var labelRow = context.Templates.CreateTextRow(root.transform, Label, false, out labelText);
            labelRow.GetComponent<LayoutElement>().preferredHeight = 30;
            var row = context.Templates.CreateEmptyRow(root.transform, 46);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(8, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            for (int i = 0; i < palette.Count; i++)
            {
                var color = palette[i];
                var go = new GameObject("Color_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
                go.transform.SetParent(row.transform, false);
                var sizing = go.GetComponent<LayoutElement>();
                sizing.preferredWidth = Mathf.Min(42, 820 / palette.Count);
                sizing.preferredHeight = 28;
                sizing.minWidth = 18;
                sizing.minHeight = 28;
                var picture = go.GetComponent<Image>();
                picture.color = color;
                var outline = go.GetComponent<Outline>();
                outline.effectDistance = new Vector2(2, -2);
                outlines.Add(outline);
                var button = go.GetComponent<Button>();
                var stateColors = button.colors;
                stateColors.normalColor = Color.white;
                stateColors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
                stateColors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                stateColors.selectedColor = Color.white;
                button.colors = stateColors;
                button.onClick.AddListener(() => Value = color);
            }
            UpdateSelection();
            return root;
        }
        public void BeginEdit() { committed = Select(getter != null ? getter() : committed); Value = committed; }
        public void Commit()
        {
            if (pending == committed) return;
            OnValueChanged?.Invoke(pending);
            committed = Select(getter != null ? getter() : pending);
            Value = committed;
        }
        public void Cancel() => BeginEdit();
    }
}

