using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Global registry for mod configuration sections displayed in Foundation's custom Config menu.</summary>
    public interface IModConfigRegistry
    {
        IReadOnlyList<IModConfigSection> Sections { get; }
        event Action Changed;
        IModConfigSection GetOrCreateSection(string modId, string modName = null, string description = null);
        bool TryGetSection(string modId, out IModConfigSection section);
        void RemoveSection(string modId);
    }

    /// <summary>Configuration section belonging to one mod.</summary>
    public interface IModConfigSection
    {
        string ModId { get; }
        string ModName { get; }
        string Description { get; set; }
        IReadOnlyList<IModConfigElement> Elements { get; }
        event Action Changed;

        IModConfigSection AddElement(IModConfigElement element);
        bool RemoveElement(IModConfigElement element);
        void ClearElements();

        // Template helper methods for standard controls:
        IModConfigText AddText(string text, bool isHeader = false);
        IModConfigButton AddButton(string label, Action onClick, string buttonText = null);
        IModConfigToggle AddToggle(string label, bool initialValue, Action<bool> onValueChanged, Func<bool> getter = null);
        IModConfigSlider AddSlider(string label, float min, float max, float initialValue, Action<float> onValueChanged, float step = 0f, Func<float> getter = null, Func<float, string> formatValue = null);
        IModConfigDropdown AddDropdown(string label, IReadOnlyList<string> options, int initialIndex, Action<int> onSelectionChanged, Func<int> getter = null);

        // BepInEx ConfigEntry bindings:
        IModConfigToggle AddToggle(BepInEx.Configuration.ConfigEntry<bool> entry, string customLabel = null);
        IModConfigSlider AddSlider(BepInEx.Configuration.ConfigEntry<int> entry, int min, int max, string customLabel = null);
        IModConfigSlider AddSlider(BepInEx.Configuration.ConfigEntry<float> entry, float min, float max, float step = 0.05f, string customLabel = null, Func<float, string> formatValue = null);
        IModConfigDropdown AddDropdown(BepInEx.Configuration.ConfigEntry<string> entry, IReadOnlyList<string> options, string customLabel = null);

        // Lifecycle notifications:
        event Action OnCommit;
        event Action OnCancel;
        void Commit();
        void Cancel();
    }

    /// <summary>Polymorphic configuration element rendered inside a mod's settings page.</summary>
    public interface IModConfigElement
    {
        string Id { get; }
        bool IsVisible { get; set; }
        GameObject CreateView(IModConfigContext context);
        void Commit();
        void Cancel();
    }

    /// <summary>Text or header element.</summary>
    public interface IModConfigText : IModConfigElement
    {
        string Text { get; set; }
        bool IsHeader { get; set; }
    }

    /// <summary>Button element with a row label and clickable button.</summary>
    public interface IModConfigButton : IModConfigElement
    {
        string Label { get; set; }
        string ButtonText { get; set; }
        Action OnClick { get; set; }
    }

    /// <summary>Boolean toggle element with a row label and checkbox toggle.</summary>
    public interface IModConfigToggle : IModConfigElement
    {
        string Label { get; set; }
        bool Value { get; set; }
        Action<bool> OnValueChanged { get; set; }
    }

    /// <summary>Numeric slider element with a row label, slider bar, and value display text.</summary>
    public interface IModConfigSlider : IModConfigElement
    {
        string Label { get; set; }
        float MinValue { get; set; }
        float MaxValue { get; set; }
        float Step { get; set; }
        float Value { get; set; }
        Action<float> OnValueChanged { get; set; }
        Func<float, string> FormatValue { get; set; }
    }

    /// <summary>Dropdown selection element with a row label and native popup dropdown.</summary>
    public interface IModConfigDropdown : IModConfigElement
    {
        string Label { get; set; }
        IReadOnlyList<string> Options { get; set; }
        int SelectedIndex { get; set; }
        Action<int> OnSelectionChanged { get; set; }
    }

    /// <summary>Context passed to elements when creating their view.</summary>
    public interface IModConfigContext
    {
        RectTransform Container { get; }
        IModConfigTemplates Templates { get; }
    }

    /// <summary>Factory for native-styled configuration rows and elements cloned from vanilla Options UI.</summary>
    public interface IModConfigTemplates
    {
        GameObject CreateButtonRow(Transform parent, string label, string buttonText, Action onClick, out Button button, out TMP_Text buttonLabel);
        GameObject CreateToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged, out Toggle toggle);
        GameObject CreateSliderRow(Transform parent, string label, float min, float max, float initialValue, float step, Action<float> onChanged, Func<float, string> formatValue, out Slider slider, out TMP_Text valueText);
        GameObject CreateDropdownRow(Transform parent, string label, IReadOnlyList<string> options, int initialIndex, Action<int> onChanged, out TMP_Dropdown dropdown);
        GameObject CreateTextRow(Transform parent, string text, bool isHeader, out TMP_Text textComponent);
        GameObject CreateEmptyRow(Transform parent, float height = 38f);
    }
}
