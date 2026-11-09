using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Text or header element.</summary>
    public interface IModConfigText : IModConfigElement
    {
        string Text { get; set; }
        bool IsHeader { get; set; }
    }
}
