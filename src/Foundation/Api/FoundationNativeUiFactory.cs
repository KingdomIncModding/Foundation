using System;
using Assets.Behaviour.UI.Shared;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationNativeUiFactory : IFoundationNativeUiFactory
    {
        public RectTransform CreateRect(string name, RectTransform parent)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("UI control name required.", nameof(name));
            if (!parent)
                throw new ArgumentNullException(nameof(parent));
            return TechTreeTabs.Rect(name, parent);
        }
    }
}
