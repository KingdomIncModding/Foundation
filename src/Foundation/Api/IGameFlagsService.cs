using System;

namespace KingdomFoundation.Api
{
    /// <summary>Native game Register flags, counters and strings, namespaced for each mod.</summary>
    public interface IGameFlagsService
    {
        bool HasFlag(string modId, string key, bool defaultValue = false);
        void SetFlag(string modId, string key, bool value = true);
        int GetCounter(string modId, string key, int defaultValue = 0);
        void SetCounter(string modId, string key, int value);
        int AddCounter(string modId, string key, int count = 1);
        string GetString(string modId, string key, string defaultValue = null);
        void SetString(string modId, string key, string value);
    }
}
