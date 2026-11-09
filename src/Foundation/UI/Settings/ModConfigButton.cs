using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
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
}
