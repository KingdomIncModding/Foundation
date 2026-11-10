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
        /// <summary>Lists assets currently tracked by the Foundation asset service.</summary>
        IReadOnlyList<IFoundationAsset> Assets { get; }
        /// <summary>Loads a PNG sprite under a mod-owned asset identifier; call on Unity's main thread.</summary>
        /// <param name="modId">The mod id value.</param>
        /// <param name="assetId">The asset id value.</param>
        /// <param name="absolutePngPath">The absolute png path value.</param>
        /// <param name="pixelsPerUnit">The pixels per unit value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        IFoundationAsset LoadSprite(string modId, string assetId, string absolutePngPath, float pixelsPerUnit = 100f);
        /// <summary>Loads a Unity asset bundle under a mod-owned asset identifier; call on Unity's main thread.</summary>
        /// <param name="modId">The mod id value.</param>
        /// <param name="assetId">The asset id value.</param>
        /// <param name="absoluteBundlePath">The absolute bundle path value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        IFoundationAsset LoadAssetBundle(string modId, string assetId, string absoluteBundlePath);
        /// <summary>Looks up a previously registered asset by mod identifier and asset identifier.</summary>
        /// <param name="modId">The mod id value.</param>
        /// <param name="assetId">The asset id value.</param>
        /// <param name="asset">The asset value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool TryGet(string modId, string assetId, out IFoundationAsset asset);
    }
}
