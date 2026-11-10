using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed partial class ModSaveRegistry
    {
        sealed class Registration : IDisposable
        {
            readonly ModSaveRegistry registry;
            internal string ModId;
            internal int Version;
            internal Func<JsonValue> Save;
            internal Action<int, JsonValue> Load;
            internal bool Active = true;
            // Never overwrite the retained payload if this provider could not restore it.
            internal bool LoadFailed;
            internal Registration(ModSaveRegistry registry) { this.registry = registry; }
            /// <summary>Performs the dispose operation.</summary>
            public void Dispose()
            {
                if (!Active) return;
                Active = false;
                if (registry.registrations.TryGetValue(ModId, out var current) && ReferenceEquals(current, this))
                    registry.registrations.Remove(ModId);
            }
        }
    }

}
