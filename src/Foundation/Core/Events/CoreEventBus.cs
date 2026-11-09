using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    internal sealed class CoreEventBus : ICoreEventBus
    {
        readonly object gate = new object();
        readonly Dictionary<Type, List<ICoreEventSubscription>> byType = new Dictionary<Type, List<ICoreEventSubscription>>();
        readonly CoreDiagnostics diagnostics;
        long sequence;
        internal CoreEventBus(CoreDiagnostics diagnostics) { this.diagnostics = diagnostics; }
        public IDisposable Subscribe<T>(string ownerId, Action<T> handler,
            CoreEventPriority priority = CoreEventPriority.Normal, Predicate<T> filter = null, bool once = false)
        {
            ModIdentity.Validate(ownerId);
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (gate)
            {
                if (!byType.TryGetValue(typeof(T), out var subscriptions))
                    byType.Add(typeof(T), subscriptions = new List<ICoreEventSubscription>());
                var subscription = new CoreEventSubscription<T>(this, ownerId, handler, filter, once, (int)priority, ++sequence);
                subscriptions.Add(subscription);
                return subscription;
            }
        }
        public void Publish<T>(T evt)
        {
            ICoreEventSubscription[] snapshot;
            lock (gate)
            {
                if (!byType.TryGetValue(typeof(T), out var list)) return;
                snapshot = list.OrderByDescending(x => x.Priority).ThenBy(x => x.Sequence).ToArray();
            }
            foreach (var subscription in snapshot)
            {
                try { subscription.Invoke(evt); }
                catch (Exception error)
                {
                    diagnostics.Record(CoreSeverity.Error, subscription.OwnerId, "event:" + typeof(T).Name,
                        "Subscriber callback failed; remaining subscribers will continue.", error);
                }
            }
        }
        internal void Remove(ICoreEventSubscription subscription)
        {
            lock (gate)
            {
                if (!byType.TryGetValue(subscription.EventType, out var list)) return;
                list.Remove(subscription);
                if (list.Count == 0) byType.Remove(subscription.EventType);
            }
        }
        public void ReleaseOwner(string ownerId)
        {
            ICoreEventSubscription[] snapshot;
            lock (gate) snapshot = byType.Values.SelectMany(x => x).Where(x => x.OwnerId == ownerId).ToArray();
            foreach (var subscription in snapshot) subscription.Dispose();
        }
        internal void ReleaseAll()
        {
            ICoreEventSubscription[] snapshot;
            lock (gate) snapshot = byType.Values.SelectMany(x => x).ToArray();
            foreach (var subscription in snapshot) subscription.Dispose();
        }
    }
}
