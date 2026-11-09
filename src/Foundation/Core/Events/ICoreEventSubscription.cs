using System;

namespace KingdomFoundation.Core
{
    internal interface ICoreEventSubscription : IDisposable
    {
        string OwnerId { get; }
        Type EventType { get; }
        int Priority { get; }
        long Sequence { get; }
        bool IsActive { get; }
        void Invoke(object evt);
    }
}
