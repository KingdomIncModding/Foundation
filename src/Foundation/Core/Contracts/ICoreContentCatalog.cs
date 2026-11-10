using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore content catalog API.</summary>
    public interface ICoreContentCatalog
    {
        IReadOnlyList<ContentDescriptor> Registered { get; }
        IDisposable Register(string ownerId, string localId, string kind, Version version = null,
            ContentLifetime lifetime = ContentLifetime.NativePermanent);
        bool TryGet(string id, out ContentDescriptor descriptor);
        void ReleaseOwner(string ownerId);
    }
}
