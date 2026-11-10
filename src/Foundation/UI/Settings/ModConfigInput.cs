using System;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Staged native-themed text input. No callback until Apply.</summary>
    public sealed class ModConfigInput : IModConfigElement, IModConfigEditSession
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public int MaxLength { get; }
        public Action<string> OnChanged { get; set; }
        private readonly Func<string> getter;
        private string committed;
        private string pending;
        private TMP_InputField active;

        public string Value
        {
            get => pending;
            set
            {
                pending = value ?? "";
                if (pending.Length > MaxLength) throw new ArgumentOutOfRangeException(nameof(value));
                if (active) active.SetTextWithoutNotify(pending);
            }
        }
        public ModConfigInput(string label, string value, Action<string> changed,
            Func<string> getter = null, int maxLength = 256, string id = null)
        {
            if (maxLength < 1 || maxLength > 8192) throw new ArgumentOutOfRangeException(nameof(maxLength));
            Id = id ?? Guid.NewGuid().ToString("N");
            Label = label ?? "";
            committed = pending = value ?? "";
            if (committed.Length > maxLength) throw new ArgumentOutOfRangeException(nameof(value));
            OnChanged = changed;
            this.getter = getter;
            MaxLength = maxLength;
        }
        /// <summary>Creates view.</summary>
        public GameObject CreateView(IModConfigContext context)
        {
            var view = context.Templates.CreateInputRow(context.Container, Label, pending, MaxLength,
                next => pending = next, out active);
            view.SetActive(IsVisible);
            return view;
        }
        public void BeginEdit()
        {
            committed = getter?.Invoke() ?? committed;
            Value = committed;
        }
        /// <summary>Performs the commit operation.</summary>
        public void Commit()
        {
            if (string.Equals(pending, committed, StringComparison.Ordinal)) return;
            OnChanged?.Invoke(pending);
            committed = getter?.Invoke() ?? pending;
            Value = committed;
        }
        public void Cancel() => BeginEdit();
    }
}
