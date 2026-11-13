using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GamePresentationService : IGamePresentationService
    {
        readonly HashSet<string> imageIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> textIds = new HashSet<string>(StringComparer.Ordinal);
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
            return NativeSprites.TryGetValue(iconName, out var sprite) ? sprite : null;
        }
        public IReadOnlyList<string> IconNames => Array.AsReadOnly(NativeSprites.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        public void RegisterSprite(string modId, string iconName, Sprite sprite)
        {
            string id = Name(modId, iconName);
            if (!sprite) throw new ArgumentNullException(nameof(sprite));
            if (NativeSprites.ContainsKey(id) || !imageIds.Add(id)) throw new ArgumentException("Duplicate icon: " + id);
            NativeSprites.Add(id, sprite);
        }
        public void RegisterText(string modId, string key, string locale, string value)
        {
            string id = Name(modId, key);
            if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale required.", nameof(locale));
            if (value == null) throw new ArgumentNullException(nameof(value));
            // Keep the base game's localization lookup and fallback behavior intact.
            var language = Translation.All.FirstOrDefault(l => string.Equals(l.Locale, locale, StringComparison.OrdinalIgnoreCase));
            if (language == null) throw new ArgumentException("Unknown native locale: " + locale, nameof(locale));
            var map = Lines(language);
            if (map.ContainsKey(id)) throw new ArgumentException("A translation already exists for: " + id + " / " + locale);
            map.Add(id, value);
            textIds.Add(locale + ":" + id);
        }
    }
}
