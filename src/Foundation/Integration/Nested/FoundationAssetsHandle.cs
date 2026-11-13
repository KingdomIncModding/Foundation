using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationAssetsHandle : IFoundationAssetWithDependencies
    {
        readonly FoundationAssets owner;
        internal readonly HashSet<global::KingdomFoundation.Api.FoundationAssetsHandle> Requires = new HashSet<global::KingdomFoundation.Api.FoundationAssetsHandle>();
        internal bool DependsOn(global::KingdomFoundation.Api.FoundationAssetsHandle other, HashSet<global::KingdomFoundation.Api.FoundationAssetsHandle> visited = null)
        {
            if (!IsLoaded)
                return false;
            if (Requires.Contains(other))
                return true;
            if (visited == null)
                visited = new HashSet<global::KingdomFoundation.Api.FoundationAssetsHandle>();
            if (!visited.Add(this))
                return false;
            return Requires.Any(parent => parent.DependsOn(other, visited));
        }

        public IReadOnlyList<IFoundationAsset> Dependencies => Array.AsReadOnly(Requires.Where(h => h.IsLoaded).Cast<IFoundationAsset>().ToArray());

        internal Sprite SpriteValue;
        internal Texture2D TextureValue;
        internal AssetBundle BundleValue;
        public string ModId { get; }
        public string AssetId { get; }
        public Sprite Sprite => IsLoaded ? SpriteValue : null;
        public AssetBundle Bundle => IsLoaded ? BundleValue : null;
        public bool IsLoaded { get; private set; } = true;

        internal FoundationAssetsHandle(FoundationAssets owner, string modId, string assetId)
        {
            this.owner = owner;
            ModId = modId;
            AssetId = assetId;
        }

        public void Dispose()
        {
            if (!IsLoaded)
                return;
            if (owner.assets.Values.Any(a => a.IsLoaded && !ReferenceEquals(a, this) && a.Requires.Contains(this)))
                throw new InvalidOperationException("Another loaded asset still depends on " + ModId + ":" + AssetId);
            IsLoaded = false;
            Requires.Clear();
            if (owner.assets.TryGetValue((ModId, AssetId), out global::KingdomFoundation.Api.FoundationAssetsHandle current) && ReferenceEquals(current, this))
                owner.assets.Remove((ModId, AssetId));
            // Live instantiated prefab/audio objects may still reference bundle assets.
            // Unload metadata only; the game's scene controls instance lifetime.
            if (BundleValue)
                BundleValue.Unload(false);
            if (SpriteValue)
                global::UnityEngine.Object.Destroy(SpriteValue);
            if (TextureValue)
                global::UnityEngine.Object.Destroy(TextureValue);
            SpriteValue = null;
            TextureValue = null;
            BundleValue = null;
        }
    }
}
