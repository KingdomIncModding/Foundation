using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Optional base class; implementing IModBarElement directly is equally supported.</summary>
    public abstract class ModBarElement : IModBarElement
    {
        public string Id { get; }
        public string ModId { get; }
        public float Width { get; }
        public int Order { get; set; }
        public ModBarAlignment Alignment { get; set; }
        public bool IsVisible { get; set; } = true;
        protected ModBarElement(string modId, string id, float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
        {
            ModBarRegistry.ValidateIdentity(modId, id);
            ModBarRegistry.ValidateWidth(width);
            ModBarRegistry.ValidateAlignment(alignment);
            ModId = modId; Id = id; Width = width; Order = order; Alignment = alignment;
        }
        public abstract IModBarView CreateView(IModBarContext context);
    }
}
