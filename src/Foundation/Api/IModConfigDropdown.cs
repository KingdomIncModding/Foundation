using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Dropdown selection element with a row label and native popup dropdown.</summary>
    public interface IModConfigDropdown : IModConfigElement
    {
        string Label { get; set; }
        IReadOnlyList<string> Options { get; set; }
        int SelectedIndex { get; set; }
        Action<int> OnSelectionChanged { get; set; }
    }
}
