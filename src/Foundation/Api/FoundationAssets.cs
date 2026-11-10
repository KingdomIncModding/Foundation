using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation assets type.</summary>
    internal sealed partial class FoundationAssets : IFoundationAssetDirectory
    {
        readonly Dictionary<(string, string), Handle> assets = new Dictionary<(string, string), Handle>();
        /// <summary>Gets or sets the assets value.</summary>
        public IReadOnlyList<IFoundationAsset> Assets => Array.AsReadOnly(assets.Values.Cast<IFoundationAsset>().ToArray());
        public IReadOnlyList<IFoundationAsset> ForOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return Array.Empty<IFoundationAsset>();
            return Array.AsReadOnly(assets.Values.Where(a => a.ModId == modId && a.IsLoaded)
                .OrderBy(a => a.AssetId, StringComparer.Ordinal).Cast<IFoundationAsset>().ToArray());
        }
        public int ReleaseOwner(string modId)
        {
            // Dispose dependents first. Any references from other mods keep the
            // provider's asset alive until consumers release their references.
            var released = 0;
            while (true)
            {
                var candidate = ForOwner(modId).Cast<Handle>().FirstOrDefault(a =>
                    !assets.Values.Any(other => other.IsLoaded && !ReferenceEquals(other, a) && other.Requires.Contains(a)));
                if (candidate == null) break;
                candidate.Dispose();
                released++;
            }
            return released;
        }
        /// <summary>Performs the add dependency operation.</summary>
        public void AddDependency(string ownerId, string assetId, string dependencyOwnerId, string dependencyAssetId)
        {
            if (!assets.TryGetValue((ownerId, assetId), out var dependent) || !dependent.IsLoaded)
                throw new ArgumentException("Dependent asset not registered: " + ownerId + ":" + assetId);
            if (!assets.TryGetValue((dependencyOwnerId, dependencyAssetId), out var provider) || !provider.IsLoaded)
                throw new ArgumentException("Required asset not registered: " + dependencyOwnerId + ":" + dependencyAssetId);
            if (ReferenceEquals(provider, dependent) || provider.DependsOn(dependent))
                throw new InvalidOperationException("Cyclic asset dependency rejected.");
            dependent.Requires.Add(provider);
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string modId, string assetId, out IFoundationAsset asset)
        {
            if (assets.TryGetValue((modId, assetId), out var handle)) { asset = handle; return true; }
            asset = null; return false;
        }
        static void Validate(string modId, string id, string file)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            SignalPropertyDefinition.ValidateId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(file) || !Path.IsPathRooted(file)) throw new ArgumentException("Pass an absolute asset file path.", nameof(file));
            if (!File.Exists(file)) throw new FileNotFoundException("Asset file does not exist.", file);
        }
        void CheckDuplicate(string modId, string id)
        {
            if (assets.ContainsKey((modId, id))) throw new ArgumentException("Asset ID already in use for this mod: " + id);
        }
        /// <summary>Loads sprite.</summary>
        public IFoundationAsset LoadSprite(string modId, string assetId, string absolutePngPath, float pixelsPerUnit = 100f)
        {
            Validate(modId, assetId, absolutePngPath); CheckDuplicate(modId, assetId);
            if (float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit) || pixelsPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePngPath))) throw new InvalidDataException("Invalid PNG texture file.");
                var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
                var result = new Handle(this, modId, assetId) { SpriteValue = sprite, TextureValue = texture };
                assets.Add((modId, assetId), result);
                return result;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }
        /// <summary>Loads asset bundle.</summary>
        public IFoundationAsset LoadAssetBundle(string modId, string assetId, string absoluteBundlePath)
        {
            Validate(modId, assetId, absoluteBundlePath); CheckDuplicate(modId, assetId);
            var bundle = AssetBundle.LoadFromFile(absoluteBundlePath);
            if (!bundle) throw new InvalidDataException("Unity was unable to load the AssetBundle: " + absoluteBundlePath);
            var result = new Handle(this, modId, assetId) { BundleValue = bundle };
            assets.Add((modId, assetId), result);
            return result;
        }

    }
}
