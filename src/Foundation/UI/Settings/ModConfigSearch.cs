using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Native search field and option dropdown; stages selection until Apply.</summary>
    public sealed class ModConfigSearch : IModConfigElement, IModConfigEditSession
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public IReadOnlyList<string> Options { get; }
        public Action<int> OnChanged { get; set; }
        private readonly Func<int> getter;
        private int committed;
        private int pending;
        private readonly List<int> indexes = new List<int>();
        private TMP_Dropdown active;
        private TMP_InputField search;
        public int SelectedIndex { get => pending; set => pending = value; }

        public ModConfigSearch(string label, IReadOnlyList<string> options, int index,
            Action<int> onChanged, Func<int> getter = null, string id = null)
        {
            if (options == null || options.Count == 0 || options.Any(s => s == null))
                throw new ArgumentException("Search options required", nameof(options));
            if (index < 0 || index >= options.Count) throw new ArgumentOutOfRangeException(nameof(index));
            Id = id ?? Guid.NewGuid().ToString("N");
            Label = label ?? "";
            Options = options;
            pending = committed = index;
            OnChanged = onChanged;
            this.getter = getter;
        }

        public GameObject CreateView(IModConfigContext context)
        {
            var holder = new GameObject("ConfigSearch", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            holder.transform.SetParent(context.Container, false);
            var layout = holder.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            holder.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            context.Templates.CreateInputRow(holder.transform, Label + " / Search", "", 128, _ => RefreshOptions(), out search);
            context.Templates.CreateDropdownRow(holder.transform, Label, new[] { Options[pending] }, 0,
                index => { if (index >= 0 && index < indexes.Count) pending = indexes[index]; }, out active);
            RefreshOptions();
            holder.SetActive(IsVisible);
            return holder;
        }
        private void RefreshOptions()
        {
            if (!active) return;
            indexes.Clear();
            var matches = new List<string>();
            string text = search ? search.text : "";
            for (int i = 0; i < Options.Count; i++)
                if (Options[i].IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                { indexes.Add(i); matches.Add(Options[i]); }
            active.ClearOptions();
            if (matches.Count == 0) { matches.Add("(No matches)"); active.interactable = false; }
            else active.interactable = true;
            active.AddOptions(matches);
            int selected = indexes.IndexOf(pending);
            active.SetValueWithoutNotify(Math.Max(0, selected));
            active.RefreshShownValue();
        }
        public void BeginEdit() { committed = getter?.Invoke() ?? committed; pending = committed; RefreshOptions(); }
        public void Commit()
        {
            if (pending == committed) return;
            if (pending < 0 || pending >= Options.Count) throw new InvalidOperationException("Invalid search selection.");
            OnChanged?.Invoke(pending);
            committed = getter?.Invoke() ?? pending;
            pending = committed;
            RefreshOptions();
        }
        public void Cancel() => BeginEdit();
    }
}
