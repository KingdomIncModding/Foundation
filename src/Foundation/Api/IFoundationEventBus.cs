using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Shared in-process event dispatcher. All game-originated publications run on Unity's main thread.</summary>
    public interface IFoundationEventBus
    {
        IDisposable Subscribe<T>(string modId, Action<T> handler);
        void Publish<T>(T message);
        /// <summary>Removes subscriptions registered by the specified mod and returns the number removed.</summary>
        /// <param name="modId">The mod id value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        int RemoveOwner(string modId);
        /// <summary>Returns the currently registered event subscriptions.</summary>
        IReadOnlyList<FoundationEventSubscription> Subscriptions { get; }
    }
}
