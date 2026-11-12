using System;
using Assets.Source.Util;
using LightJson;

namespace KingdomFoundation
{
    /// <summary>Compatibility for native saves that already contain the pre-1.0.14 checksum.</summary>
    [Obsolete("Deprecated: last native checksum writer was game build 1.0.13. Game 1.0.14 saves have no Secret. Scheduled for removal in the next Foundation release.", false)]
    internal static class LegacySaveChecksum
    {
        internal static void Refresh(JsonObject document)
        {
            document.Remove("Secret");
            document["Secret"] = new SeedGenerator().Add(document).Hash.ToString();
        }
    }
}
