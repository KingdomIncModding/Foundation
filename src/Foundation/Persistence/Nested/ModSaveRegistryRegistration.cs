using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class ModSaveRegistryRegistration : IDisposable
    {
        readonly ModSaveRegistry registry;
        internal string ModId;
        internal int Version;
        internal Func<JsonValue> Save;
        internal Action<int, JsonValue> Load;
        internal bool Active = true;
        // Never overwrite the retained payload if this provider could not restore it.
        internal bool LoadFailed;
        internal ModSaveRegistryRegistration(ModSaveRegistry registry)
        {
            this.registry = registry;
        }

        public void Dispose()
        {
            if (!Active)
                return;
            Active = false;
            if (registry.registrations.TryGetValue(ModId, out global::KingdomFoundation.Api.ModSaveRegistryRegistration current) && ReferenceEquals(current, this))
                registry.registrations.Remove(ModId);
        }
    }
}
