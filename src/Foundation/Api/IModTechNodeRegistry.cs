using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Additional category access without changing the original registry interface.</summary>
    public interface IModTechNodeRegistry : ITechNodeRegistry
    {
        IReadOnlyList<ITechMod> Mods { get; }
    }
}
