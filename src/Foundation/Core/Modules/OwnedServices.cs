using System;

namespace KingdomFoundation.Core
{
    internal sealed class OwnedServices : IModServices
    {
        readonly string owner;
        readonly CoreServiceRegistry services;
        readonly ModScope scope;
        internal OwnedServices(string owner, CoreServiceRegistry services, ModScope scope)
        { this.owner = owner; this.services = services; this.scope = scope; }
        public IDisposable Register<T>(string serviceId, T instance, Version version = null,
            CoreLifetime lifetime = CoreLifetime.Module, bool disposeInstance = false) where T : class
        {
            scope.EnsureActive();
            return scope.Track(services.Register(owner, serviceId, instance, version, lifetime, disposeInstance));
        }
        public bool TryResolve<T>(string serviceId, out T service, Version minimumVersion = null) where T : class
            => services.TryResolve(serviceId, out service, minimumVersion);
        public T Resolve<T>(string serviceId, Version minimumVersion = null) where T : class
            => services.Resolve<T>(serviceId, minimumVersion);
        public bool TryResolve(Type type, string id, out object service, Version minimumVersion = null)
            => services.TryResolve(type, id, out service, minimumVersion);
    }
}
