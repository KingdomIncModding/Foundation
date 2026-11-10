using System;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.Shared;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        sealed class ProbeBarElement : IModBarElement
        {
            public string Id => "lifetime";
            public string ModId => "foundation.diagnostics";
            public float Width => 180;
            public int Order => 10;
            /// <summary>Gets or sets the alignment value.</summary>
            public ModBarAlignment Alignment => ModBarAlignment.Right;
            public bool IsVisible => true;
            internal int Creates, Refreshes, Disposes;
            internal Vector2 InitialSize;
            public IModBarView CreateView(IModBarContext context) { Creates++; InitialSize = context.Root.rect.size; return new ProbeView(this, context); }
            sealed class ProbeView : IModBarView
            {
                readonly ProbeBarElement element;
                readonly TMP_Text text;
                internal ProbeView(ProbeBarElement element, IModBarContext context) { this.element = element; text = context.CreateText("Custom contract"); }
                /// <summary>Performs the refresh operation.</summary>
                public void Refresh() { element.Refreshes++; text.text = "Custom contract"; }
                public void Dispose() { element.Disposes++; }
            }
        }
    }

}
