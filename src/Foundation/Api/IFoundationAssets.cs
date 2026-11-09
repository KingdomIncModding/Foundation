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
        IFoundationAsset LoadAssetBundle(string modId, string assetId, string absoluteBundlePath);
        bool TryGet(string modId, string assetId, out IFoundationAsset asset);
    }
}
