using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Configuration section belonging to one mod.</summary>
    public interface IModConfigSection
    {
        string ModId { get; }
        string ModName { get; }
        string Description { get; set; }
        IReadOnlyList<IModConfigElement> Elements { get; }
        event Action Changed;

        /// <summary>Performs the add element operation.</summary>
        IModConfigSection AddElement(IModConfigElement element);
        bool RemoveElement(IModConfigElement element);
        void ClearElements();

        // Template helper methods for standard controls:
        ModConfigInput AddInput(string label, string initialValue, Action<string> onChanged,
            Func<string> getter = null, int maxLength = 256);
        ModConfigNumber AddNumber(string label, double minimum, double maximum, double value,
            double step, Action<double> onChanged, Func<double> getter = null);
        /// <summary>Performs the add search operation.</summary>
        ModConfigSearch AddSearch(string label, IReadOnlyList<string> options,
            int initialIndex, Action<int> onChanged, Func<int> getter = null);
        IModConfigText AddText(string text, bool isHeader = false);
        IModConfigButton AddButton(string label, Action onClick, string buttonText = null);
        IModConfigToggle AddToggle(string label, bool initialValue, Action<bool> onValueChanged, Func<bool> getter = null);
        IModConfigSlider AddSlider(string label, float min, float max, float initialValue, Action<float> onValueChanged, float step = 0f, Func<float> getter = null, Func<float, string> formatValue = null);
        IModConfigDropdown AddDropdown(string label, IReadOnlyList<string> options, int initialIndex, Action<int> onSelectionChanged, Func<int> getter = null);

        // BepInEx ConfigEntry bindings:
        /// <summary>Performs the add toggle operation.</summary>
        IModConfigToggle AddToggle(BepInEx.Configuration.ConfigEntry<bool> entry, string customLabel = null);
        IModConfigSlider AddSlider(BepInEx.Configuration.ConfigEntry<int> entry, int min, int max, string customLabel = null);
        IModConfigSlider AddSlider(BepInEx.Configuration.ConfigEntry<float> entry, float min, float max, float step = 0.05f, string customLabel = null, Func<float, string> formatValue = null);
        IModConfigDropdown AddDropdown(BepInEx.Configuration.ConfigEntry<string> entry, IReadOnlyList<string> options, string customLabel = null);

        // Edits to toggles, sliders and dropdowns are staged until Apply.
        // Callbacks run only for values that changed at commit time, not on each
        // interaction. Cancel restores the previously committed values.
        // Lifecycle notifications:
        /// <summary>Occurs when on commit is raised.</summary>
        event Action OnCommit;
        event Action OnCancel;
        void Commit();
        void Cancel();
    }
}
