using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    public sealed class ModBarText : ModBarElement
    {
        public string Text { get; set; }
        public ModBarText(string modId, string id, string text, float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment) { Text = text; }
        public override IModBarView CreateView(IModBarContext context)
        { var text = context.CreateText(Text); return new ModBarControlView(() => text.text = Text ?? ""); }
    }
}
