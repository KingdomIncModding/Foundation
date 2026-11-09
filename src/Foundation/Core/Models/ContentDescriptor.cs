using System;

namespace KingdomFoundation.Core
{
    public sealed class ContentDescriptor
    {
        public string Id { get; }
        public string OwnerId { get; }
        public string Kind { get; }
        public Version Version { get; }
        public ContentLifetime Lifetime { get; }
        public bool OwnerActive { get; internal set; }
        /// <summary>A native item/quest/technology remains reserved after its owner stops.</summary>
        public bool IsReserved => Lifetime == ContentLifetime.NativePermanent;
        internal ContentDescriptor(string id, string ownerId, string kind, Version version, ContentLifetime lifetime)
        {
            Id = id;
            OwnerId = ownerId;
            Kind = kind;
            Version = version;
            Lifetime = lifetime;
            OwnerActive = true;
        }
    }
}
