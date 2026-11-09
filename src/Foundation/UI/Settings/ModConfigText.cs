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
}
