using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    public class ModConfigDropdown : IModConfigDropdown, IModConfigEditSession
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
            var go = context.Templates.CreateDropdownRow(context.Container, Label, Options, pendingIndex, idx => {
                pendingIndex = idx;
            }, out activeDropdown);
            go.SetActive(IsVisible);
            return go;
        }

        public void BeginEdit()
        {
            if (getter != null) committedIndex = getter();
            SelectedIndex = committedIndex;
        }

        public void Commit()
        {
            if (pendingIndex == committedIndex) return;
            OnSelectionChanged?.Invoke(pendingIndex);
            committedIndex = getter != null ? getter() : pendingIndex;
            SelectedIndex = committedIndex;
        }

        public void Cancel() => BeginEdit();
    }
}
