using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>A polymorphic control definition. CreateView is called once for each HUD lifetime.</summary>
    public interface IModBarElement
    {
        string Id { get; }
        string ModId { get; }
        float Width { get; }
        int Order { get; }
        ModBarAlignment Alignment { get; }
        bool IsVisible { get; }
        IModBarView CreateView(IModBarContext context);
    }
}
