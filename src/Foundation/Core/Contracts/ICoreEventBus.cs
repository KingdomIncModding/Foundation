using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore event bus API.</summary>
    public interface ICoreEventBus
    {
        IDisposable Subscribe<T>(string ownerId, Action<T> handler, CoreEventPriority priority = CoreEventPriority.Normal,
            Predicate<T> filter = null, bool once = false);
        void Publish<T>(T evt);
        void ReleaseOwner(string ownerId);
    }
}
