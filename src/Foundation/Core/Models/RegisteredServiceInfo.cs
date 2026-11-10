using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the registered service info API.</summary>
    public sealed class RegisteredServiceInfo
    {
        public string OwnerId { get; }
        public string Id { get; }
        public Type ContractType { get; }
        public Version Version { get; }
        public CoreLifetime Lifetime { get; }
        public RegisteredServiceInfo(string ownerId, string id, Type contractType, Version version, CoreLifetime lifetime)
        { OwnerId = ownerId; Id = id; ContractType = contractType; Version = version; Lifetime = lifetime; }
    }
}
