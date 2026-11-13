using System;
using System.Threading;

namespace KingdomFoundation.Core
{
    internal sealed class CoreEventSubscription<T> : ICoreEventSubscription
    {
        readonly CoreEventBus bus;
        readonly Action<T> handler;
        readonly Predicate<T> filter;
        readonly bool once;
        int active = 1;
        public string OwnerId { get; }
        public Type EventType => typeof(T);
        public int Priority { get; }
        public long Sequence { get; }
        public bool IsActive => Volatile.Read(ref active) != 0;
        internal CoreEventSubscription(CoreEventBus bus, string ownerId, Action<T> handler,
            Predicate<T> filter, bool once, int priority, long sequence)
        {
            this.bus = bus;
            this.handler = handler;
            this.filter = filter;
            this.once = once;
            OwnerId = ownerId;
            Priority = priority;
            Sequence = sequence;
        }
        public void Invoke(object evt)
        {
            if (!IsActive) return;
            if (filter != null && !filter((T)evt)) return;
            try { handler((T)evt); }
            finally { if (once) Dispose(); }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref active, 0) == 0) return;
            bus.Remove(this);
        }
    }
}
