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
        /// <summary>Provides the remove owner operation.</summary>
        /// <param name="modId">The mod id argument.</param>
        /// <returns>The operation result.</returns>
        int RemoveOwner(string modId);
        IReadOnlyList<FoundationEventSubscription> Subscriptions { get; }
    }
}
