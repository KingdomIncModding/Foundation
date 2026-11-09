using System;

namespace KingdomFoundation.Core
{
    internal sealed class OwnedDispatcher : IModDispatcher
    {
        readonly string ownerId;
        readonly ModScope scope;
        readonly CoreDispatcher dispatcher;
        internal OwnedDispatcher(string ownerId, CoreDispatcher dispatcher, ModScope scope)
        { this.ownerId = ownerId; this.dispatcher = dispatcher; this.scope = scope; }
        public bool IsMainThread => dispatcher.IsMainThread;
        public void RequireMainThread() => dispatcher.RequireMainThread();
        public IDisposable Post(Action callback)
        {
            scope.EnsureActive();
            return scope.Track(dispatcher.Post(ownerId, callback));
        }
    }
}
