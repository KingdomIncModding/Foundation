using System;

namespace KingdomFoundation.Core
{
    public interface IModServices
    {
        IDisposable Register<T>(string serviceId, T instance, Version version = null,
            CoreLifetime lifetime = CoreLifetime.Module, bool disposeInstance = false) where T : class;
        bool TryResolve<T>(string serviceId, out T service, Version minimumVersion = null) where T : class;
        T Resolve<T>(string serviceId, Version minimumVersion = null) where T : class;
        bool TryResolve(Type contractType, string serviceId, out object service, Version minimumVersion = null);
    }
}
