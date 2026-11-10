using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod bar separator API.</summary>
    public sealed class ModBarSeparator : ModBarElement
    {
        public ModBarSeparator(string modId, string id, float width = 16, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment) { }
        public ModBarSeparator(string modId, string id, ModBarAlignment alignment, int order = 0, float width = 16)
            : base(modId, id, width, order, alignment) { }
        public override IModBarView CreateView(IModBarContext context)
        {
            context.CreateSeparator();
            return new ModBarControlView(null);
        }
    }
}
