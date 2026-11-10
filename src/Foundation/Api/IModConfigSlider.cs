using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Numeric slider element with a row label, slider bar, and value display text.</summary>
    public interface IModConfigSlider : IModConfigElement
    {
        string Label { get; set; }
        float MinValue { get; set; }
        float MaxValue { get; set; }
        float Step { get; set; }
        float Value { get; set; }
        Action<float> OnValueChanged { get; set; }
        /// <summary>Gets or sets the format value value.</summary>
        Func<float, string> FormatValue { get; set; }
    }
}
