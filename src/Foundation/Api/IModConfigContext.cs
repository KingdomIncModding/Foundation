using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Context passed to elements when creating their view.</summary>
    public interface IModConfigContext
    {
        RectTransform Container { get; }
        IModConfigTemplates Templates { get; }
    }
}
