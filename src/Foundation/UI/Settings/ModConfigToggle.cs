using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod config toggle API.</summary>
    public class ModConfigToggle : IModConfigToggle, IModConfigEditSession
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

        /// <summary>Creates view.</summary>
        public GameObject CreateView(IModConfigContext context)
        {
            var go = context.Templates.CreateToggleRow(context.Container, Label, pendingValue, val => {
                pendingValue = val;
            }, out activeToggle);
            go.SetActive(IsVisible);
            return go;
        }

        /// <summary>Performs the begin edit operation.</summary>
        public void BeginEdit()
        {
            if (getter != null) committedValue = getter();
            pendingValue = committedValue;
            if (activeToggle != null) activeToggle.SetIsOnWithoutNotify(pendingValue);
        }

        public void Commit()
        {
            if (pendingValue == committedValue) return;
            OnValueChanged?.Invoke(pendingValue);
            committedValue = getter != null ? getter() : pendingValue;
            pendingValue = committedValue;
        }

        /// <summary>Performs the cancel operation.</summary>
        public void Cancel() => BeginEdit();
    }
}
