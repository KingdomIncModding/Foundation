using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationAssets : IFoundationAssetDirectory
    {
        internal readonly Dictionary<(string, string), global::KingdomFoundation.Api.FoundationAssetsHandle> assets = new Dictionary<(string, string), global::KingdomFoundation.Api.FoundationAssetsHandle>();
        public IReadOnlyList<IFoundationAsset> Assets => Array.AsReadOnly(assets.Values.Cast<IFoundationAsset>().ToArray());

        public IReadOnlyList<IFoundationAsset> ForOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return Array.Empty<IFoundationAsset>();
            return Array.AsReadOnly(assets.Values.Where(a => a.ModId == modId && a.IsLoaded).OrderBy(a => a.AssetId, StringComparer.Ordinal).Cast<IFoundationAsset>().ToArray());
        }

        public int ReleaseOwner(string modId)
        {
            // Dispose dependents first. Any references from other mods keep the
            // provider's asset alive until consumers release their references.
            var released = 0;
            while (true)
            {
                global::KingdomFoundation.Api.FoundationAssetsHandle candidate = ForOwner(modId).Cast<global::KingdomFoundation.Api.FoundationAssetsHandle>().FirstOrDefault(a => !assets.Values.Any(other => other.IsLoaded && !ReferenceEquals(other, a) && other.Requires.Contains(a)));
                if (candidate == null)
                    break;
                candidate.Dispose();
                released++;
            }

            return released;
        }

        public void AddDependency(string ownerId, string assetId, string dependencyOwnerId, string dependencyAssetId)
        {
            if (!assets.TryGetValue((ownerId, assetId), out global::KingdomFoundation.Api.FoundationAssetsHandle dependent) || !dependent.IsLoaded)
                throw new ArgumentException("Dependent asset not registered: " + ownerId + ":" + assetId);
            if (!assets.TryGetValue((dependencyOwnerId, dependencyAssetId), out global::KingdomFoundation.Api.FoundationAssetsHandle provider) || !provider.IsLoaded)
                throw new ArgumentException("Required asset not registered: " + dependencyOwnerId + ":" + dependencyAssetId);
            if (ReferenceEquals(provider, dependent) || provider.DependsOn(dependent))
                throw new InvalidOperationException("Cyclic asset dependency rejected.");
            dependent.Requires.Add(provider);
        }

        public bool TryGet(string modId, string assetId, out IFoundationAsset asset)
        {
            if (assets.TryGetValue((modId, assetId), out global::KingdomFoundation.Api.FoundationAssetsHandle handle))
            {
                asset = handle;
                return true;
            }

            asset = null;
            return false;
        }

        static void Validate(string modId, string id, string file)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            SignalPropertyDefinition.ValidateId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(file) || !Path.IsPathRooted(file))
                throw new ArgumentException("Pass an absolute asset file path.", nameof(file));
            if (!File.Exists(file))
                throw new FileNotFoundException("Asset file does not exist.", file);
        }

        void CheckDuplicate(string modId, string id)
        {
            if (assets.ContainsKey((modId, id)))
                throw new ArgumentException("Asset ID already in use for this mod: " + id);
        }

        public IFoundationAsset LoadSprite(string modId, string assetId, string absolutePngPath, float pixelsPerUnit = 100f)
        {
            Validate(modId, assetId, absolutePngPath);
            CheckDuplicate(modId, assetId);
            if (float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit) || pixelsPerUnit <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePngPath)))
                    throw new InvalidDataException("Invalid PNG texture file.");
                var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
                global::KingdomFoundation.Api.FoundationAssetsHandle result = new global::KingdomFoundation.Api.FoundationAssetsHandle(this, modId, assetId)
                {
                    SpriteValue = sprite,
                    TextureValue = texture
                };
                assets.Add((modId, assetId), result);
                return result;
            }
            catch
            {
                global::UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        public IFoundationAsset LoadAssetBundle(string modId, string assetId, string absoluteBundlePath)
        {
            Validate(modId, assetId, absoluteBundlePath);
            CheckDuplicate(modId, assetId);
            var bundle = AssetBundle.LoadFromFile(absoluteBundlePath);
            if (!bundle)
                throw new InvalidDataException("Unity was unable to load the AssetBundle: " + absoluteBundlePath);
            global::KingdomFoundation.Api.FoundationAssetsHandle result = new global::KingdomFoundation.Api.FoundationAssetsHandle(this, modId, assetId)
            {
                BundleValue = bundle
            };
            assets.Add((modId, assetId), result);
            return result;
        }
    }
}
