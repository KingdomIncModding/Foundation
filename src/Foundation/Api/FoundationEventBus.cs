using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Core;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Compatibility facade for the existing FoundationApi.Events contract.
    /// CoreEventBus now does the ACTUAL delivery for both Core and legacy
    /// subscribers. This facade only tracks the tokens it owns so old
    /// FoundationModContext.RemoveOwner semantics are preserved.
    /// </summary>
    internal sealed class FoundationEventBus : IFoundationEventBus
    {
        readonly object gate = new object ();
        readonly Dictionary<Type, List<global::KingdomFoundation.Api.FoundationEventBusEntry>> subscriptions = new Dictionary<Type, List<global::KingdomFoundation.Api.FoundationEventBusEntry>>();
        public IReadOnlyList<FoundationEventSubscription> Subscriptions
        {
            get
            {
                lock (gate)
                    return subscriptions.SelectMany(pair => pair.Value.Where(e => e.IsActive).Select(e => new FoundationEventSubscription(e.ModId, pair.Key))).ToArray();
            }
        }

        public IDisposable Subscribe<T>(string modId, Action<T> handler)
        {
            if (string.IsNullOrWhiteSpace(modId))
                throw new ArgumentException("Stable mod GUID required.", nameof(modId));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            // Subscriptions enter the SAME bus as scoped Core subscriptions.
            // No second publication and no second native event dispatch.
            var token = FoundationCoreApi.Core.Events.Subscribe(modId, handler);
            global::KingdomFoundation.Api.FoundationEventBusEntry entry = new global::KingdomFoundation.Api.FoundationEventBusEntry
            {
                Owner = this,
                ModId = modId,
                Type = typeof(T),
                Callback = handler,
                Token = token
            };
            lock (gate)
            {
                if (!subscriptions.TryGetValue(typeof(T), out var list))
                    subscriptions[typeof(T)] = list = new List<global::KingdomFoundation.Api.FoundationEventBusEntry>();
                list.Add(entry);
            }

            return entry;
        }

        public void Publish<T>(T message) => FoundationCoreApi.Core.Events.Publish(message);
        public int RemoveOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return 0;
            global::KingdomFoundation.Api.FoundationEventBusEntry[] removed;
            lock (gate)
                removed = subscriptions.Values.SelectMany(x => x).Where(x => x.Active && x.ModId == modId).ToArray();
            foreach (global::KingdomFoundation.Api.FoundationEventBusEntry entry in removed)
                entry.Dispose();
            return removed.Length;
        }

        internal void Unsubscribe(global::KingdomFoundation.Api.FoundationEventBusEntry entry)
        {
            IDisposable token;
            lock (gate)
            {
                if (!entry.Active)
                    return;
                entry.Active = false;
                if (subscriptions.TryGetValue(entry.Type, out var list))
                {
                    list.Remove(entry);
                    if (list.Count == 0)
                        subscriptions.Remove(entry.Type);
                }

                token = entry.Token;
                entry.Token = null;
            }

            // Dispose OUTSIDE the facade lock: the shared Core bus has its
            // own lock and could otherwise invoke unrelated callbacks reentrantly.
            token?.Dispose();
        }
    }
}
