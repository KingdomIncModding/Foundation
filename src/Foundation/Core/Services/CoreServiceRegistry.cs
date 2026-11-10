using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    internal sealed class CoreServiceRegistry : ICoreServices, ICoreServiceDirectory
    {
        readonly object gate = new object();
        readonly Dictionary<string, CoreServiceEntry> entries = new Dictionary<string, CoreServiceEntry>(StringComparer.Ordinal);
        readonly CoreDiagnostics diagnostics;
        readonly CoreEventBus events;
        Func<bool> worldAvailable;
        internal CoreServiceRegistry(CoreDiagnostics diagnostics, CoreEventBus events)
        { this.diagnostics = diagnostics; this.events = events; }
        internal void SetWorldAvailability(Func<bool> provider) { worldAvailable = provider; }
        internal void Report(string id, string operation, Exception error)
            => diagnostics.Record(CoreSeverity.Error, id, operation, "Service cleanup failed.", error);

        static string Key(Type type, string id) => type.AssemblyQualifiedName + "|" + id;

        public IReadOnlyList<RegisteredServiceInfo> Registered
        {
            get { lock (gate) return Array.AsReadOnly(entries.Values.Select(x => x.Info).ToArray()); }
        }
        /// <summary>Discover actual registered contract providers; no parallel registry.</summary>
        public IReadOnlyList<RegisteredServiceInfo> Find(Type contractType, Version minimumVersion = null)
        {
            if (contractType == null) throw new ArgumentNullException(nameof(contractType));
            lock (gate) return Array.AsReadOnly(entries.Values
                .Where(x => x.Info.ContractType == contractType &&
                    (minimumVersion == null || x.Info.Version >= minimumVersion))
                .Select(x => x.Info).OrderBy(x => x.OwnerId, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal).ToArray());
        }
        /// <summary>Finds owner.</summary>
        public IReadOnlyList<RegisteredServiceInfo> FindOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return Array.AsReadOnly(Array.Empty<RegisteredServiceInfo>());
            lock (gate) return Array.AsReadOnly(entries.Values
                .Where(x => string.Equals(x.Info.OwnerId, ownerId, StringComparison.Ordinal))
                .Select(x => x.Info).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
        }
        public bool TryResolveProvider<T>(string providerId, out T service, Version minimumVersion = null) where T : class
        {
            service = null;
            if (string.IsNullOrWhiteSpace(providerId)) return false;
            // Resolution and discovery use the same map and require the EXACT
            // registered contract, not an unsafe reflection-based cast.
            lock (gate)
            {
                // Do not silently pick the first provider when it registered
                // multiple services under the same shared contract. Consumers
                // must use Find(Type) followed by the exact service ID then.
                var matches = entries.Values.Where(x =>
                    x.Info.ContractType == typeof(T) &&
                    x.Info.OwnerId == providerId &&
                    (minimumVersion == null || x.Info.Version >= minimumVersion))
                    .Take(2).ToArray();
                if (matches.Length != 1) return false;
                service = (T)matches[0].Instance;
                return true;
            }
        }
        public IDisposable Register<T>(string ownerId, string serviceId, T instance, Version version = null,
            CoreLifetime lifetime = CoreLifetime.Module, bool disposeInstance = false) where T : class
        {
            ModIdentity.Validate(ownerId);
            if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID required.", nameof(serviceId));
            if (!serviceId.StartsWith(ownerId + ":", StringComparison.Ordinal))
                throw new ArgumentException("Service IDs must start with the owner's mod ID followed by ':'", nameof(serviceId));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (version != null && version.Major < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (!Enum.IsDefined(typeof(CoreLifetime), lifetime)) throw new ArgumentOutOfRangeException(nameof(lifetime));
            if (lifetime == CoreLifetime.World && (worldAvailable == null || !worldAvailable()))
                throw new InvalidOperationException("World-lifetime services require an active kingdom.");
            string key = Key(typeof(T), serviceId);
            var entry = new CoreServiceEntry(this,
                new RegisteredServiceInfo(ownerId, serviceId, typeof(T), version ?? new Version(1, 0), lifetime),
                instance, disposeInstance);
            lock (gate)
            {
                if (entries.ContainsKey(key)) throw new InvalidOperationException("Service already registered: " + serviceId);
                entries.Add(key, entry);
            }
            events.Publish(new ServiceRegisteredEvent(entry.Info));
            return entry;
        }
        public bool TryResolve<T>(string serviceId, out T service, Version minimumVersion = null) where T : class
        {
            bool found = TryResolve(typeof(T), serviceId, out var raw, minimumVersion);
            service = found ? (T)raw : null;
            return found;
        }
        public T Resolve<T>(string serviceId, Version minimumVersion = null) where T : class
        {
            if (TryResolve<T>(serviceId, out var service, minimumVersion)) return service;
            throw new KeyNotFoundException("Service not registered or incompatible: " + serviceId);
        }
        /// <summary>Attempts to resolve.</summary>
        public bool TryResolve(Type contractType, string serviceId, out object service, Version minimumVersion = null)
        {
            if (contractType == null) throw new ArgumentNullException(nameof(contractType));
            if (string.IsNullOrWhiteSpace(serviceId)) { service = null; return false; }
            lock (gate)
            {
                if (entries.TryGetValue(Key(contractType, serviceId), out var entry) &&
                    (minimumVersion == null || entry.Info.Version >= minimumVersion))
                {
                    service = entry.Instance;
                    return true;
                }
            }
            service = null;
            return false;
        }
        internal void Remove(CoreServiceEntry entry)
        {
            bool removed = false;
            lock (gate)
            {
                var key = Key(entry.Info.ContractType, entry.Info.Id);
                if (entries.TryGetValue(key, out var found) && ReferenceEquals(found, entry))
                    removed = entries.Remove(key);
            }
            if (removed) events.Publish(new ServiceRemovedEvent(entry.Info));
        }
        /// <summary>Performs the release owner operation.</summary>
        public void ReleaseOwner(string ownerId) => ReleaseWhere(x => x.Info.OwnerId == ownerId);
        internal void ReleaseWorld() => ReleaseWhere(x => x.Info.Lifetime == CoreLifetime.World);
        internal void ReleaseAll() => ReleaseWhere(x => true);
        void ReleaseWhere(Func<CoreServiceEntry, bool> predicate)
        {
            CoreServiceEntry[] snapshot;
            lock (gate) snapshot = entries.Values.Where(predicate).ToArray();
            // Release outside the lock, since disposed instances can call back into services.
            foreach (var entry in snapshot) entry.Dispose();
        }
    }
}
