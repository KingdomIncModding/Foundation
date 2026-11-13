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
    internal sealed class ProbeBarElement : IModBarElement
    {
        public string Id => "lifetime";
        public string ModId => "foundation.diagnostics";
        public float Width => 180;
        public int Order => 10;
        public ModBarAlignment Alignment => ModBarAlignment.Right;
        public bool IsVisible => true;

        internal int Creates, Refreshes, Disposes;
        internal Vector2 InitialSize;
        public IModBarView CreateView(IModBarContext context)
        {
            Creates++;
            InitialSize = context.Root.rect.size;
            return new global::KingdomFoundation.ProbeBarElement.ProbeView(this, context);
        }

        sealed class ProbeView : IModBarView
        {
            readonly global::KingdomFoundation.ProbeBarElement element;
            readonly TMP_Text text;
            internal ProbeView(global::KingdomFoundation.ProbeBarElement element, IModBarContext context)
            {
                this.element = element;
                text = context.CreateText("Custom contract");
            }

            public void Refresh()
            {
                element.Refreshes++;
                text.text = "Custom contract";
            }

            public void Dispose()
            {
                element.Disposes++;
            }
        }
    }
}
