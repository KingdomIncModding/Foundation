using System;

namespace KingdomFoundation.Core
{
    internal sealed class OwnedEvents : IModEvents
    {
        readonly string owner;
        readonly CoreEventBus events;
        readonly ModScope scope;
        internal OwnedEvents(string owner, CoreEventBus events, ModScope scope)
        { this.owner = owner; this.events = events; this.scope = scope; }
        public IDisposable Subscribe<T>(Action<T> callback, CoreEventPriority priority = CoreEventPriority.Normal,
            Predicate<T> filter = null, bool once = false)
        {
            scope.EnsureActive();
            return scope.Track(events.Subscribe(owner, callback, priority, filter, once));
        }
        public void Publish<T>(T evt)
        {
            scope.EnsureActive();
            events.Publish(evt);
        }
    }
}
