using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the imod config color swatches API.</summary>
    public interface IModConfigColorSwatches : IModConfigElement
    {
        string Label { get; set; }
        IReadOnlyList<Color> Palette { get; }
        Color Value { get; set; }
        Action<Color> OnValueChanged { get; set; }
    }
}
