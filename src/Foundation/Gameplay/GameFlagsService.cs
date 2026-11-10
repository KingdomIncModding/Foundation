using System;

namespace KingdomFoundation.Api
{
    internal sealed class GameFlagsService : IGameFlagsService
    {
        static string Key(string modId, string key)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Both mod ID and key are required.");
            if (key.Contains(":")) throw new ArgumentException("Key must not contain ':'.", nameof(key));
            GameWorldService.RequirePlayer();
            return "foundation:" + modId + ":" + key;
        }
        /// <summary>Performs the has flag operation.</summary>
        public bool HasFlag(string modId, string key, bool defaultValue = false) => Register.HasFlag(Key(modId, key), defaultValue);
        public void SetFlag(string modId, string key, bool value = true) => Register.SetFlag(Key(modId, key), value);
        public int GetCounter(string modId, string key, int defaultValue = 0) => Register.GetCounter(Key(modId, key), defaultValue);
        public void SetCounter(string modId, string key, int value) => Register.SetCounter(Key(modId, key), value);
        public int AddCounter(string modId, string key, int count = 1) => Register.AddCounter(Key(modId, key), count);
        public string GetString(string modId, string key, string defaultValue = null) => Register.GetData(Key(modId, key), defaultValue);
        public void SetString(string modId, string key, string value) => Register.SetData(Key(modId, key), value);
    }
}
