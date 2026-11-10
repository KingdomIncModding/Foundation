using System;
using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    /// <summary>Additive asset ownership/dependency interface. The legacy IFoundationAssets ABI remains unchanged.</summary>
    public interface IFoundationAssetDirectory : IFoundationAssets
    {
        IReadOnlyList<IFoundationAsset> ForOwner(string modId);
        int ReleaseOwner(string modId);
        /// <summary>Provides the add dependency operation.</summary>
        /// <param name="ownerId">The owner id argument.</param>
        /// <param name="assetId">The asset id argument.</param>
        /// <param name="dependencyOwnerId">The dependency owner id argument.</param>
        /// <param name="dependencyAssetId">The dependency asset id argument.</param>
        void AddDependency(string ownerId, string assetId, string dependencyOwnerId, string dependencyAssetId);
    }
    /// <summary>Optional dependency introspection. Existing IFoundationAsset implementations need no new members.</summary>
    public interface IFoundationAssetWithDependencies : IFoundationAsset
    {
        IReadOnlyList<IFoundationAsset> Dependencies { get; }
    }
}
