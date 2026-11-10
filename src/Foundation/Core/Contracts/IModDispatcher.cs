using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the imod dispatcher API.</summary>
    public interface IModDispatcher
    {
        bool IsMainThread { get; }
        void RequireMainThread();
        IDisposable Post(Action callback);
    }
}
