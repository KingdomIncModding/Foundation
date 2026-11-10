using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the imod tech node API.</summary>
    public interface IModTechNode : ITechNode
    {
        ITechMod Mod { get; }
    }
}
