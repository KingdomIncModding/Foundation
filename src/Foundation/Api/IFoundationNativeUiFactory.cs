using System;
using Assets.Behaviour.UI.Shared;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Native UI element creation bridge for Foundation-owned mod controls.
    /// Uses Kingdom Inc's existing TechTreeTabs Rect factory; no second Canvas or style manager.</summary>
    public interface IFoundationNativeUiFactory
    {
        RectTransform CreateRect(string name, RectTransform parent);
    }
    internal sealed class FoundationNativeUiFactory : IFoundationNativeUiFactory
    {
        public RectTransform CreateRect(string name, RectTransform parent)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("UI control name required.", nameof(name));
            if (!parent) throw new ArgumentNullException(nameof(parent));
            return TechTreeTabs.Rect(name, parent);
        }
    }
}
