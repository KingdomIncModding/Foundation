using System;
using System.Threading;

namespace KingdomFoundation.Core
{
    internal sealed class CoreServiceEntry : IDisposable
    {
        readonly CoreServiceRegistry registry;
        readonly bool disposeInstance;
        int disposed;
        internal object Instance { get; }
        internal RegisteredServiceInfo Info { get; }
        internal CoreServiceEntry(CoreServiceRegistry registry, RegisteredServiceInfo info, object instance, bool disposeInstance)
        {
            this.registry = registry;
            this.disposeInstance = disposeInstance;
            Info = info;
            Instance = instance;
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            registry.Remove(this);
            if (!disposeInstance || !(Instance is IDisposable owned)) return;
            try { owned.Dispose(); }
            catch (Exception ex) { registry.Report(Info.OwnerId, "service-dispose", ex); }
        }
    }
}
