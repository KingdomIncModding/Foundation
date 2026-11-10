using System;

namespace KingdomFoundation.Core
{
    internal sealed class CoreContentEntry : IDisposable
    {
        readonly CoreContentCatalog owner;
        internal ContentDescriptor Descriptor { get; }
        internal CoreContentEntry(CoreContentCatalog owner, ContentDescriptor descriptor)
        { this.owner = owner; Descriptor = descriptor; }
        /// <summary>Performs the dispose operation.</summary>
        public void Dispose()
        {
            // Native content is backed by static game registries and persistent IDs.
            // It CANNOT be removed from a running kingdom without losing save semantics.
            if (Descriptor.Lifetime != ContentLifetime.NativePermanent) owner.Remove(this);
        }
    }
}
