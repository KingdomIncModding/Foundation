using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class LoadSnapshot
    {
        internal HashSet<string> MissingIds;
        internal readonly Dictionary<string, JsonValue> MissingNotifications = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
    }
}
