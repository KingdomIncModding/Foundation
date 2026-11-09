using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Factory for native-styled configuration rows and elements cloned from vanilla Options UI.</summary>
    public interface IModConfigTemplates
    {
        GameObject CreateButtonRow(Transform parent, string label, string buttonText, Action onClick, out Button button, out TMP_Text buttonLabel);
        GameObject CreateToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged, out Toggle toggle);
        GameObject CreateSliderRow(Transform parent, string label, float min, float max, float initialValue, float step, Action<float> onChanged, Func<float, string> formatValue, out Slider slider, out TMP_Text valueText);
        GameObject CreateDropdownRow(Transform parent, string label, IReadOnlyList<string> options, int initialIndex, Action<int> onChanged, out TMP_Dropdown dropdown);
        GameObject CreateInputRow(Transform parent, string label, string currentValue, int maxLength, Action<string> onEdited, out TMP_InputField input);
        GameObject CreateTextRow(Transform parent, string text, bool isHeader, out TMP_Text textComponent);
        GameObject CreateEmptyRow(Transform parent, float height = 38f);
    }
}
