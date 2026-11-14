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
        /// <summary>Prevents workspace zoom while the pointer is over this UI.</summary>
        void BlockWorkspaceZoom(GameObject view);
    }
}
