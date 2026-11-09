using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>A mod's category in the research sidebar. Empty categories are omitted from the registry.</summary>
    public interface ITechMod
    {
        string Id { get; }
        string Name { get; }
        IReadOnlyList<ITechNode> Nodes { get; }
    }
}
