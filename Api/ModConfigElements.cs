using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    public class ModConfigText : IModConfigText
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Text { get; set; }
        public bool IsHeader { get; set; }
        TMP_Text activeText;

        public ModConfigText(string text, bool isHeader = false, string id = null)
        {
            Text = text ?? "";
            IsHeader = isHeader;
            Id = id ?? Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            var go = context.Templates.CreateTextRow(context.Container, Text, IsHeader, out activeText);
            go.SetActive(IsVisible);
            return go;
        }

        public void Commit() { }
        public void Cancel() { }
    }

    public class ModConfigButton : IModConfigButton
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public string ButtonText { get; set; }
        public Action OnClick { get; set; }

        public ModConfigButton(string label, Action onClick, string buttonText = null, string id = null)
        {
            Label = label ?? "";
            OnClick = onClick;
            ButtonText = buttonText ?? label;
            Id = id ?? Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            var go = context.Templates.CreateButtonRow(context.Container, Label, ButtonText, () => OnClick?.Invoke(), out _, out _);
            go.SetActive(IsVisible);
            return go;
        }

        public void Commit() { }
        public void Cancel() { }
    }

    public class ModConfigToggle : IModConfigToggle
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public Action<bool> OnValueChanged { get; set; }
        readonly Func<bool> getter;
        bool committedValue;
        bool pendingValue;
        Toggle activeToggle;

        public bool Value
        {
            get => pendingValue;
            set
            {
                pendingValue = value;
                if (activeToggle != null) activeToggle.SetIsOnWithoutNotify(value);
            }
        }

        public ModConfigToggle(string label, bool initialValue, Action<bool> onValueChanged, Func<bool> getter = null, string id = null)
        {
            Label = label ?? "";
            committedValue = initialValue;
            pendingValue = initialValue;
            OnValueChanged = onValueChanged;
            this.getter = getter;
            Id = id ?? Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            if (getter != null) { committedValue = getter(); pendingValue = committedValue; }
            var go = context.Templates.CreateToggleRow(context.Container, Label, pendingValue, val => {
                pendingValue = val;
                OnValueChanged?.Invoke(val);
            }, out activeToggle);
            go.SetActive(IsVisible);
            return go;
        }

        public void Commit()
        {
            committedValue = pendingValue;
            OnValueChanged?.Invoke(committedValue);
        }

        public void Cancel()
        {
            if (getter != null) committedValue = getter();
            pendingValue = committedValue;
            if (activeToggle != null) activeToggle.SetIsOnWithoutNotify(committedValue);
        }
    }

    public class ModConfigSlider : IModConfigSlider
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float Step { get; set; }
        public Action<float> OnValueChanged { get; set; }
        public Func<float, string> FormatValue { get; set; }
        readonly Func<float> getter;
        float committedValue;
        float pendingValue;
        Slider activeSlider;
        TMP_Text activeValueText;

        public float Value
        {
            get => pendingValue;
            set
            {
                pendingValue = value;
                if (activeSlider != null) activeSlider.SetValueWithoutNotify(value);
                if (activeValueText != null) activeValueText.text = FormatValue != null ? FormatValue(value) : value.ToString("0.##");
            }
        }

        public ModConfigSlider(string label, float min, float max, float initialValue, Action<float> onValueChanged, float step = 0f, Func<float> getter = null, Func<float, string> formatValue = null, string id = null)
        {
            Label = label ?? "";
            MinValue = min;
            MaxValue = max;
            Step = step;
            committedValue = initialValue;
            pendingValue = initialValue;
            OnValueChanged = onValueChanged;
            this.getter = getter;
            FormatValue = formatValue;
            Id = id ?? Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            if (getter != null) { committedValue = getter(); pendingValue = committedValue; }
            var go = context.Templates.CreateSliderRow(context.Container, Label, MinValue, MaxValue, pendingValue, Step, val => {
                pendingValue = val;
                OnValueChanged?.Invoke(val);
            }, FormatValue, out activeSlider, out activeValueText);
            go.SetActive(IsVisible);
            return go;
        }

        public void Commit()
        {
            committedValue = pendingValue;
            OnValueChanged?.Invoke(committedValue);
        }

        public void Cancel()
        {
            if (getter != null) committedValue = getter();
            pendingValue = committedValue;
            if (activeSlider != null)
            {
                activeSlider.SetValueWithoutNotify(committedValue);
                if (activeValueText != null)
                    activeValueText.text = FormatValue != null ? FormatValue(committedValue) : committedValue.ToString("0.##");
            }
        }
    }

    public class ModConfigDropdown : IModConfigDropdown
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public IReadOnlyList<string> Options { get; set; }
        public Action<int> OnSelectionChanged { get; set; }
        readonly Func<int> getter;
        int committedIndex;
        int pendingIndex;
        TMP_Dropdown activeDropdown;

        public int SelectedIndex
        {
            get => pendingIndex;
            set
            {
                pendingIndex = value;
                if (activeDropdown != null) activeDropdown.SetValueWithoutNotify(value);
            }
        }

        public ModConfigDropdown(string label, IReadOnlyList<string> options, int initialIndex, Action<int> onSelectionChanged, Func<int> getter = null, string id = null)
        {
            Label = label ?? "";
            Options = options ?? new string[0];
            committedIndex = initialIndex;
            pendingIndex = initialIndex;
            OnSelectionChanged = onSelectionChanged;
            this.getter = getter;
            Id = id ?? Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            if (getter != null) { committedIndex = getter(); pendingIndex = committedIndex; }
            var go = context.Templates.CreateDropdownRow(context.Container, Label, Options, pendingIndex, idx => {
                pendingIndex = idx;
                OnSelectionChanged?.Invoke(idx);
            }, out activeDropdown);
            go.SetActive(IsVisible);
            return go;
        }

        public void Commit()
        {
            committedIndex = pendingIndex;
            OnSelectionChanged?.Invoke(committedIndex);
        }

        public void Cancel()
        {
            if (getter != null) committedIndex = getter();
            pendingIndex = committedIndex;
            if (activeDropdown != null) activeDropdown.SetValueWithoutNotify(committedIndex);
        }
    }
}
