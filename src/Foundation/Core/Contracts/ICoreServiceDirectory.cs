using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Inter-mod service discovery against the same authoritative service registry.</summary>
    public interface ICoreServiceDirectory
    {
        IReadOnlyList<RegisteredServiceInfo> Find(Type contractType, Version minimumVersion = null);
        IReadOnlyList<RegisteredServiceInfo> FindOwner(string ownerId);
        bool TryResolveProvider<T>(string providerId, out T service, Version minimumVersion = null) where T : class;
    }
}
