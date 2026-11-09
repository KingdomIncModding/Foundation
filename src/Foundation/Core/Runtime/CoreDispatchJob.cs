using System;
using System.Threading;

namespace KingdomFoundation.Core
{
    internal sealed class CoreDispatchJob : IDisposable
    {
        int state;
        internal string OwnerId { get; }
        internal Action Callback { get; }
        internal CoreDispatchJob(string ownerId, Action callback)
        { OwnerId = ownerId; Callback = callback; }
        internal bool TryRun() => Interlocked.CompareExchange(ref state, 1, 0) == 0;
        public void Dispose() => Interlocked.Exchange(ref state, 1);
    }
}
