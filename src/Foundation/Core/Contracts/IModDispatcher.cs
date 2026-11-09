using System;

namespace KingdomFoundation.Core
{
    public interface IModDispatcher
    {
        bool IsMainThread { get; }
        void RequireMainThread();
        IDisposable Post(Action callback);
    }
}
