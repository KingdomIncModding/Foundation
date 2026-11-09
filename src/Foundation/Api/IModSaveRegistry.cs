using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>Per-kingdom persistence embedded in the game's save JSON, not a separate unsynchronized sidecar.</summary>
    public interface IModSaveRegistry
    {
        /// <summary>Register a stable mod GUID, current schema, and serialization callbacks. Register during plugin Awake.</summary>
        IDisposable Register(string modId, int schemaVersion, Func<JsonValue> save, Action<int, JsonValue> load);
        bool TryGetLastLoaded(string modId, out int schemaVersion, out JsonValue state);
        IReadOnlyList<string> RetainedModIds { get; }
    }
}
