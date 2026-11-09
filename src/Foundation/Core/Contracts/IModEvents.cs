using System;

namespace KingdomFoundation.Core
{
    public interface IModEvents
    {
        IDisposable Subscribe<T>(Action<T> handler, CoreEventPriority priority = CoreEventPriority.Normal,
            Predicate<T> filter = null, bool once = false);
        void Publish<T>(T evt);
    }
}
