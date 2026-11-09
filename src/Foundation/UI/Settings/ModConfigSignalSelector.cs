using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Native-themed paired selectors: signal source followed by its exposed property.</summary>
    public sealed class ModConfigSignalSelector : IModConfigElement, IModConfigEditSession
    {
        public string Id { get; }
        public bool IsVisible { get; set; } = true;
        public string Label { get; set; }
        public SignalSelection Value { get => pending; set { pending = value ?? new SignalSelection(null, null); RefreshOptions(); } }
        public Action<SignalSelection> OnChanged { get; set; }
        readonly Func<SignalSelection> getter;
        readonly List<string> sources = new List<string>();
        readonly List<string> properties = new List<string>();
        SignalSelection committed;
        SignalSelection pending;
        TMP_Dropdown sourceDropdown;
        TMP_Dropdown propertyDropdown;

        public ModConfigSignalSelector(string label, SignalSelection initialValue, Action<SignalSelection> onChanged,
            Func<SignalSelection> getter = null, string id = null)
        {
            Label = label ?? "Signal Source";
            Id = id ?? Guid.NewGuid().ToString("N");
            committed = initialValue ?? new SignalSelection(null, null);
            pending = committed;
            OnChanged = onChanged;
            this.getter = getter;
        }
        public GameObject CreateView(IModConfigContext context)
        {
            var parent = new GameObject("FoundationSignalSelector", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
            var rt = (RectTransform)parent.transform;
            rt.SetParent(context.Container, false);
            var vertical = parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;
            parent.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            // Temporary host implementing the native templates' expected container interface.
            var src = context.Templates.CreateDropdownRow(rt, Label + " — Source", new[] { "(None)" }, 0, index =>
            {
                var selected = index >= 0 && index < sources.Count ? sources[index] : null;
                pending = new SignalSelection(selected, null);
                RefreshOptions();
            }, out sourceDropdown);
            var prop = context.Templates.CreateDropdownRow(rt, "Exposed Property", new[] { "(None)" }, 0, index =>
            {
                var selected = index >= 0 && index < properties.Count ? properties[index] : null;
                pending = new SignalSelection(pending.SourceId, selected);
            }, out propertyDropdown);
            src.SetActive(true); prop.SetActive(true);
            RefreshOptions();
            parent.SetActive(IsVisible);
            return parent;
        }
        private static string FriendlyCaption(string label, string fallback, int maxLength = 28)
        {
            var display = string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
            if (display.Length <= maxLength) return display;
            return display.Substring(0, maxLength - 3).TrimEnd() + "...";
        }

        void RefreshOptions()
        {
            sources.Clear();
            var sourceLabels = new List<string> { "(None)" };
            sources.Add(null);
            var catalog = FoundationApi.Signals.Sources.ToArray();
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var source in catalog)
            {
                var name = FriendlyCaption(source.Name, "Signal Source");
                nameCounts[name] = nameCounts.TryGetValue(name, out var count) ? count + 1 : 1;
            }
            var nameOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var source in catalog)
            {
                sources.Add(source.Id);
                // Do not expose ModId or a GUID in native Options captions.
                // Duplicate user-friendly names are distinguished by a short
                // ordinal suffix; the IDs remain in the parallel sources list.
                var name = FriendlyCaption(source.Name, "Signal Source");
                nameOrdinals[name] = nameOrdinals.TryGetValue(name, out var ordinal) ? ordinal + 1 : 1;
                var suffix = nameCounts[name] > 1 ? " (" + nameOrdinals[name] + ")" : "";
                sourceLabels.Add(FriendlyCaption(name, "Signal Source", 28 - suffix.Length) + suffix);
            }
            if (sourceDropdown != null)
            {
                sourceDropdown.ClearOptions();
                sourceDropdown.AddOptions(sourceLabels);
                sourceDropdown.SetValueWithoutNotify(Math.Max(0, sources.IndexOf(pending.SourceId)));
                sourceDropdown.RefreshShownValue();
            }
            properties.Clear();
            var labels = new List<string> { "(None)" };
            properties.Add(null);
            if (FoundationApi.Signals.TryGet(pending.SourceId, out var sourceObj))
            {
                var propCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in sourceObj.Properties)
                {
                    var name = FriendlyCaption(property.Name, "Property");
                    propCounts[name] = propCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }
                var propOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in sourceObj.Properties)
                {
                    properties.Add(property.Id);
                    var name = FriendlyCaption(property.Name, "Property");
                    propOrdinals[name] = propOrdinals.TryGetValue(name, out var ordinal) ? ordinal + 1 : 1;
                    var suffix = propCounts[name] > 1 ? " (" + propOrdinals[name] + ")" : "";
                    labels.Add(FriendlyCaption(name, "Property", 28 - suffix.Length) + suffix);
                }
            }
            if (propertyDropdown != null)
            {
                propertyDropdown.ClearOptions();
                propertyDropdown.AddOptions(labels);
                propertyDropdown.SetValueWithoutNotify(Math.Max(0, properties.IndexOf(pending.PropertyId)));
                propertyDropdown.interactable = properties.Count > 1;
                propertyDropdown.RefreshShownValue();
            }
        }
        public void BeginEdit() { committed = getter?.Invoke() ?? committed; pending = committed; RefreshOptions(); }
        public void Cancel() => BeginEdit();
        public void Commit()
        {
            if (committed.Equals(pending)) return;
            // Never silently commit a property belonging to a different source.
            if (pending.SourceId != null && !FoundationApi.Signals.TryGet(pending.SourceId, out var source))
                throw new InvalidOperationException("Selected signal source no longer exists: " + pending.SourceId);
            if (pending.PropertyId != null && (pending.SourceId == null || !FoundationApi.Signals.TryGet(pending.SourceId, out var candidate) || !candidate.TryGetProperty(pending.PropertyId, out _)))
                throw new InvalidOperationException("Selected signal property no longer exists: " + pending.PropertyId);
            OnChanged?.Invoke(pending);
            committed = getter?.Invoke() ?? pending;
            pending = committed;
            RefreshOptions();
        }
    }
}
