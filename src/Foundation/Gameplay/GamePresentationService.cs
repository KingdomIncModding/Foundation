using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KingdomFoundation.Api
{
    internal sealed class GamePresentationService : IGamePresentationService
    {
        private const int MaxNegativeSpriteCacheEntries = 512;
        readonly Dictionary<string, HashSet<string>> ownerSpriteIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        readonly Dictionary<string, List<(string Locale, string Key)>> ownerTextKeys = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
        readonly Dictionary<string, Sprite> fallbackSprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        readonly HashSet<string> missingFallbackSprites = new HashSet<string>(StringComparer.Ordinal);
        readonly Queue<string> missingFallbackOrder = new Queue<string>();
        bool sceneHooksAttached;
        internal int FullSpriteScanCount { get; private set; }

        internal GamePresentationService()
        {
            AttachSceneInvalidation();
        }

        void AttachSceneInvalidation()
        {
            if (sceneHooksAttached)
                return;
            sceneHooksAttached = true;
            SceneManager.sceneLoaded += (_, __) => InvalidateSpriteLookupCache();
            SceneManager.sceneUnloaded += _ => InvalidateSpriteLookupCache();
        }

        internal void InvalidateSpriteLookupCache()
        {
            fallbackSprites.Clear();
            missingFallbackSprites.Clear();
            missingFallbackOrder.Clear();
        }

        static Dictionary<string, Sprite> NativeSprites =>
            (Dictionary<string, Sprite>)AccessTools.Field(typeof(SpriteLibrary), "Sprites").GetValue(null);
        static Dictionary<string, string> Lines(Translation translation) =>
            (Dictionary<string, string>)AccessTools.Field(typeof(Translation), "_lines").GetValue(translation);

        static string Name(string owner, string local)
        {
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(local))
                throw new ArgumentException("Owner GUID and local name must both be provided.");
            if (local.Contains(":")) throw new ArgumentException("Local name may not contain ':'.", nameof(local));
            return owner + ":" + local;
        }

        public string CurrentLocale => Translation.Current == null ? Translation.DefaultLanguage : Translation.CurrentLocale;
        public IReadOnlyList<string> NativeLocales
            => Array.AsReadOnly(Translation.All.Select(locale => locale.Locale).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        public string TranslateOnly(string token, params object[] arguments)
            => Translation.TranslateOnly(token, arguments);
        public string Translate(string token, params object[] arguments) => Translation.Translate(token, arguments);

        public Sprite FindSprite(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            if (NativeSprites.TryGetValue(iconName, out var sprite) && sprite) return sprite;

            if (fallbackSprites.TryGetValue(iconName, out var cached))
            {
                if (cached && string.Equals(cached.name, iconName, StringComparison.Ordinal))
                    return cached;
                fallbackSprites.Remove(iconName);
            }

            if (missingFallbackSprites.Contains(iconName))
                return null;

            FullSpriteScanCount++;
            Sprite matched = null;
            // Index all currently loaded sprites on a scan so subsequent queries avoid rescanning.
            foreach (var loaded in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (!loaded || string.IsNullOrEmpty(loaded.name)) continue;
                if (!fallbackSprites.ContainsKey(loaded.name))
                {
                    fallbackSprites[loaded.name] = loaded;
                    missingFallbackSprites.Remove(loaded.name);
                }
                if (matched == null && string.Equals(loaded.name, iconName, StringComparison.Ordinal))
                    matched = loaded;
            }

            if (matched != null)
                return matched;

            RecordMissingSprite(iconName);
            return null;
        }

        void RecordMissingSprite(string iconName)
        {
            if (missingFallbackSprites.Contains(iconName))
                return;
            while (missingFallbackOrder.Count >= MaxNegativeSpriteCacheEntries)
            {
                var evicted = missingFallbackOrder.Dequeue();
                missingFallbackSprites.Remove(evicted);
            }
            missingFallbackSprites.Add(iconName);
            missingFallbackOrder.Enqueue(iconName);
        }

        public IReadOnlyList<string> IconNames => Array.AsReadOnly(NativeSprites.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());

        public void RegisterSprite(string modId, string iconName, Sprite sprite)
        {
            string id = Name(modId, iconName);
            if (!sprite) throw new ArgumentNullException(nameof(sprite));
            if (NativeSprites.ContainsKey(id)) throw new ArgumentException("Duplicate icon: " + id);
            if (!ownerSpriteIds.TryGetValue(modId, out var owned))
            {
                owned = new HashSet<string>(StringComparer.Ordinal);
                ownerSpriteIds[modId] = owned;
            }
            if (!owned.Add(id)) throw new ArgumentException("Duplicate icon: " + id);
            NativeSprites.Add(id, sprite);
            missingFallbackSprites.Remove(id);
            fallbackSprites.Remove(id);
        }

        public void RegisterText(string modId, string key, string locale, string value)
        {
            string id = Name(modId, key);
            if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale required.", nameof(locale));
            if (value == null) throw new ArgumentNullException(nameof(value));
            var language = Translation.All.FirstOrDefault(l => string.Equals(l.Locale, locale, StringComparison.OrdinalIgnoreCase));
            if (language == null) throw new ArgumentException("Unknown native locale: " + locale, nameof(locale));
            var map = Lines(language);
            if (map.ContainsKey(id)) throw new ArgumentException("A translation already exists for: " + id + " / " + locale);
            map.Add(id, value);
            if (!ownerTextKeys.TryGetValue(modId, out var owned))
            {
                owned = new List<(string, string)>();
                ownerTextKeys[modId] = owned;
            }
            owned.Add((language.Locale, id));
        }

        internal void RemoveOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return;

            if (ownerSpriteIds.TryGetValue(modId, out var spriteIds))
            {
                var nativeSprites = NativeSprites;
                foreach (var id in spriteIds)
                {
                    nativeSprites.Remove(id);
                    fallbackSprites.Remove(id);
                    missingFallbackSprites.Remove(id);
                }
                ownerSpriteIds.Remove(modId);
            }

            if (ownerTextKeys.TryGetValue(modId, out var textEntries))
            {
                foreach (var (locale, key) in textEntries)
                {
                    var language = Translation.All.FirstOrDefault(l => string.Equals(l.Locale, locale, StringComparison.OrdinalIgnoreCase));
                    if (language != null)
                        Lines(language).Remove(key);
                }
                ownerTextKeys.Remove(modId);
            }
        }
    }
}
