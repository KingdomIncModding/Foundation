using System.Collections.Generic;
using HarmonyLib;

namespace KingdomFoundation.Json
{
    internal static class JsonObjectStorage
    {
        internal static readonly AccessTools.FieldRef<LightJson.JsonObject, IDictionary<string,LightJson.JsonValue>> Properties =
            AccessTools.FieldRefAccess<LightJson.JsonObject, IDictionary<string,LightJson.JsonValue>>("properties");
    }
}
