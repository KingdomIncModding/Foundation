using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    internal sealed class CoreContentCatalog : ICoreContentCatalog
    {
        readonly object gate = new object();
        readonly Dictionary<string, CoreContentEntry> entries = new Dictionary<string, CoreContentEntry>(StringComparer.Ordinal);
        Func<bool> isWorldActive;
        readonly CoreEventBus events;
        internal CoreContentCatalog(CoreEventBus events) { this.events = events; }
        internal void SetWorldAvailability(Func<bool> provider) => isWorldActive = provider;
        public IReadOnlyList<ContentDescriptor> Registered
        {
            get { lock (gate) return Array.AsReadOnly(entries.Values.Select(x => x.Descriptor).ToArray()); }
        }
        /// <summary>Performs the register operation.</summary>
        public IDisposable Register(string ownerId, string localId, string kind, Version version = null,
            ContentLifetime lifetime = ContentLifetime.NativePermanent)
        {
            ModIdentity.Validate(ownerId);
            if (string.IsNullOrWhiteSpace(localId) || localId.IndexOf(':') >= 0)
                throw new ArgumentException("Local content ID must be nonempty and contain no ':'.", nameof(localId));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Content kind required.", nameof(kind));
            var id = ownerId + ":" + localId;
            if (lifetime == ContentLifetime.World && (isWorldActive == null || !isWorldActive()))
                throw new InvalidOperationException("World content needs an active kingdom.");
            var entry = new CoreContentEntry(this,
                new ContentDescriptor(id, ownerId, kind, version ?? new Version(1, 0), lifetime));
            lock (gate)
            {
                if (entries.ContainsKey(id)) throw new InvalidOperationException("Content ID already registered: " + id);
                entries.Add(id, entry);
            }
            events.Publish(new ContentRegisteredEvent(entry.Descriptor));
            return entry;
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string id, out ContentDescriptor descriptor)
        {
            lock (gate)
            {
                if (id != null && entries.TryGetValue(id, out var entry))
                { descriptor = entry.Descriptor; return true; }
            }
            descriptor = null;
            return false;
        }
        internal void Remove(CoreContentEntry entry)
        {
            bool removed = false;
            lock (gate)
            {
                if (entries.TryGetValue(entry.Descriptor.Id, out var current) && ReferenceEquals(entry, current))
                    removed = entries.Remove(entry.Descriptor.Id);
            }
            if (removed) events.Publish(new ContentReleasedEvent(entry.Descriptor));
        }
        /// <summary>Performs the release owner operation.</summary>
        public void ReleaseOwner(string ownerId)
        {
            CoreContentEntry[] remove;
            lock (gate)
            {
                remove = entries.Values.Where(x => x.Descriptor.OwnerId == ownerId &&
                    x.Descriptor.Lifetime != ContentLifetime.NativePermanent).ToArray();
                foreach (var entry in entries.Values)
                    if (entry.Descriptor.OwnerId == ownerId) entry.Descriptor.OwnerActive = false;
            }
            foreach (var entry in remove) entry.Dispose();
        }
        internal void ActivateOwner(string ownerId)
        {
            lock (gate)
                foreach (var item in entries.Values)
                    if (item.Descriptor.OwnerId == ownerId) item.Descriptor.OwnerActive = true;
        }
        internal void ReleaseWorld()
        {
            CoreContentEntry[] remove;
            lock (gate) remove = entries.Values.Where(x => x.Descriptor.Lifetime == ContentLifetime.World).ToArray();
            foreach (var entry in remove) entry.Dispose();
        }
    }
}
