using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the imod content API.</summary>
    public interface IModContent
    {
        IDisposable Register(string localId, string kind, Version version = null,
            ContentLifetime lifetime = ContentLifetime.NativePermanent);
        bool TryGet(string fullId, out ContentDescriptor descriptor);
    }
}
