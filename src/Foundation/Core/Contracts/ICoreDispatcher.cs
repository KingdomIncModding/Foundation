using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore dispatcher API.</summary>
    public interface ICoreDispatcher
    {
        bool IsMainThread { get; }
        void RequireMainThread();
        IDisposable Post(string ownerId, Action callback);
        void CancelOwner(string ownerId);
    }
}
