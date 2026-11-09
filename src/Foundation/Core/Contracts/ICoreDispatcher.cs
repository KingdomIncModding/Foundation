using System;

namespace KingdomFoundation.Core
{
    public interface ICoreDispatcher
    {
        bool IsMainThread { get; }
        void RequireMainThread();
        IDisposable Post(string ownerId, Action callback);
        void CancelOwner(string ownerId);
    }
}
