using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Framework-owned slot and native-style factories. Custom controls must remain children of Root.</summary>
    public interface IModBarContext
    {
        RectTransform Root { get; }
        TMP_Text CreateText(string text);
        Button CreateButton(string text, Action clicked);
        TMP_Dropdown CreateDropdown(IReadOnlyList<string> options, int selectedIndex, Action<int> selected);
        Toggle CreateToggle(string text, bool value, Action<bool> changed);
        Image CreateSeparator();
    }
}
