using System;

namespace KingdomFoundation.Core
{
    internal sealed class OwnedContent : IModContent
    {
        readonly string ownerId;
        readonly CoreContentCatalog catalog;
        readonly ModScope scope;
        internal OwnedContent(string ownerId, CoreContentCatalog catalog, ModScope scope)
        { this.ownerId = ownerId; this.catalog = catalog; this.scope = scope; }
        /// <summary>Performs the register operation.</summary>
        public IDisposable Register(string localId, string kind, Version version = null,
            ContentLifetime lifetime = ContentLifetime.NativePermanent)
        {
            scope.EnsureActive();
            var token = catalog.Register(ownerId, localId, kind, version, lifetime);
            if (lifetime != ContentLifetime.NativePermanent) return scope.Track(token);
            return token;
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string fullId, out ContentDescriptor descriptor)
            => catalog.TryGet(fullId, out descriptor);
    }
}
