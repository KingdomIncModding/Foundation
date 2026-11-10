using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed partial class FoundationAssets
    {
        sealed class Handle : IFoundationAssetWithDependencies
        {
            readonly FoundationAssets owner;
            internal readonly HashSet<Handle> Requires = new HashSet<Handle>();
            internal bool DependsOn(Handle other, HashSet<Handle> visited = null)
            {
                if (!IsLoaded) return false;
                if (Requires.Contains(other)) return true;
                if (visited == null) visited = new HashSet<Handle>();
                if (!visited.Add(this)) return false;
                return Requires.Any(parent => parent.DependsOn(other, visited));
            }
            /// <summary>Gets or sets the dependencies value.</summary>
            public IReadOnlyList<IFoundationAsset> Dependencies => Array.AsReadOnly(
                Requires.Where(h => h.IsLoaded).Cast<IFoundationAsset>().ToArray());
            internal Sprite SpriteValue;
            internal Texture2D TextureValue;
            internal AssetBundle BundleValue;
            public string ModId { get; }
            public string AssetId { get; }
            public Sprite Sprite => IsLoaded ? SpriteValue : null;
            /// <summary>Gets or sets the bundle value.</summary>
            public AssetBundle Bundle => IsLoaded ? BundleValue : null;
            public bool IsLoaded { get; private set; } = true;
            internal Handle(FoundationAssets owner, string modId, string assetId)
            { this.owner = owner; ModId = modId; AssetId = assetId; }
            public void Dispose()
            {
                if (!IsLoaded) return;
                if (owner.assets.Values.Any(a => a.IsLoaded && !ReferenceEquals(a, this) && a.Requires.Contains(this)))
                    throw new InvalidOperationException("Another loaded asset still depends on " + ModId + ":" + AssetId);
                IsLoaded = false;
                Requires.Clear();
                if (owner.assets.TryGetValue((ModId, AssetId), out var current) && ReferenceEquals(current, this))
                    owner.assets.Remove((ModId, AssetId));
                // Live instantiated prefab/audio objects may still reference bundle assets.
                // Unload metadata only; the game's scene controls instance lifetime.
                if (BundleValue) BundleValue.Unload(false);
                if (SpriteValue) UnityEngine.Object.Destroy(SpriteValue);
                if (TextureValue) UnityEngine.Object.Destroy(TextureValue);
                SpriteValue = null; TextureValue = null; BundleValue = null;
            }
        }
    }

}
