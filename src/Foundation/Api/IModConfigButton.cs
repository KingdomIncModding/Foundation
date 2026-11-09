using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Button element with a row label and clickable button.</summary>
    public interface IModConfigButton : IModConfigElement
    {
        string Label { get; set; }
        string ButtonText { get; set; }
        Action OnClick { get; set; }
    }
}
