using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Optional dependency introspection. Existing IFoundationAsset implementations need no new members.</summary>
    public interface IFoundationAssetWithDependencies : IFoundationAsset
    {
        IReadOnlyList<IFoundationAsset> Dependencies { get; }
    }
}
