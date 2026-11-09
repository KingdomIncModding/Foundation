using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Boolean toggle element with a row label and checkbox toggle.</summary>
    public interface IModConfigToggle : IModConfigElement
    {
        string Label { get; set; }
        bool Value { get; set; }
        Action<bool> OnValueChanged { get; set; }
    }
}
