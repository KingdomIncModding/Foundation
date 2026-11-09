using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    public interface IModTechNode : ITechNode
    {
        ITechMod Mod { get; }
    }
}
