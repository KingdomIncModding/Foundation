using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Stable asset ownership keyed by mod GUID + asset ID; Unity object operations require main thread.</summary>
    public interface IFoundationAssets
    {
        IReadOnlyList<IFoundationAsset> Assets { get; }
        IFoundationAsset LoadSprite(string modId, string assetId, string absolutePngPath, float pixelsPerUnit = 100f);
        /// <summary>Provides the load asset bundle operation.</summary>
        /// <param name="modId">The mod id argument.</param>
        /// <param name="assetId">The asset id argument.</param>
        /// <param name="absoluteBundlePath">The absolute bundle path argument.</param>
        /// <returns>The operation result.</returns>
        IFoundationAsset LoadAssetBundle(string modId, string assetId, string absoluteBundlePath);
        bool TryGet(string modId, string assetId, out IFoundationAsset asset);
    }
}
