using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod config slider API.</summary>
    public class ModConfigSlider : IModConfigSlider, IModConfigEditSession
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float Step { get; set; }
        /// <summary>Gets or sets the on value changed value.</summary>
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

        /// <summary>Creates view.</summary>
        public GameObject CreateView(IModConfigContext context)
        {
            var go = context.Templates.CreateSliderRow(context.Container, Label, MinValue, MaxValue, pendingValue, Step, val => {
                pendingValue = val;
            }, FormatValue, out activeSlider, out activeValueText);
            go.SetActive(IsVisible);
            return go;
        }

        /// <summary>Performs the begin edit operation.</summary>
        public void BeginEdit()
        {
            if (getter != null) committedValue = getter();
            Value = committedValue;
        }

        public void Commit()
        {
            if (Math.Abs(pendingValue - committedValue) < 0.000001f) return;
            OnValueChanged?.Invoke(pendingValue);
            committedValue = getter != null ? getter() : pendingValue;
            Value = committedValue;
        }

        /// <summary>Performs the cancel operation.</summary>
        public void Cancel() => BeginEdit();
    }
}
