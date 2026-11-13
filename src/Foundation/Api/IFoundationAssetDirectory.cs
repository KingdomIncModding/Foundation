using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Additive asset ownership/dependency interface. The legacy IFoundationAssets ABI remains unchanged.</summary>
    public interface IFoundationAssetDirectory : IFoundationAssets
    {
        IReadOnlyList<IFoundationAsset> ForOwner(string modId);
        int ReleaseOwner(string modId);
        void AddDependency(string ownerId, string assetId, string dependencyOwnerId, string dependencyAssetId);
    }
}
