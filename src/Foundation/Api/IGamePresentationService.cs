using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Use the game's real SpriteLibrary and Translation dictionaries for mod content.</summary>
    public interface IGamePresentationService
    {
        string CurrentLocale { get; }
        IReadOnlyList<string> NativeLocales { get; }
        /// <summary>Invokes the translate only operation.</summary>
        /// <param name="token">The token argument.</param>
        /// <param name="arguments">The arguments argument.</param>
        /// <returns>The operation result.</returns>
        string TranslateOnly(string token, params object[] arguments);

        string Translate(string token, params object[] arguments);
        Sprite FindSprite(string iconName);
        /// <summary>Gets or sets the icon names value.</summary>
        IReadOnlyList<string> IconNames { get; }
        void RegisterSprite(string modId, string iconName, Sprite sprite);
        void RegisterText(string modId, string key, string locale, string value);
    }
}
