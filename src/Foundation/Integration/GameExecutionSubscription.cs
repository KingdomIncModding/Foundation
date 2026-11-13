using System;

namespace KingdomFoundation.Api
{
    internal sealed class GameExecutionSubscription : IDisposable
    {
        internal readonly string ModId;
        internal readonly Func<GameExecutionCall, object> Begin;
        internal readonly Action<GameExecutionCall, object> End;
        private readonly Action<GameExecutionSubscription> remove;
        internal volatile bool Active = true;
        internal GameExecutionSubscription(string modId, Func<GameExecutionCall, object> begin, Action<GameExecutionCall, object> end, Action<GameExecutionSubscription> remove)
        { ModId = modId; Begin = begin; End = end; this.remove = remove; }
        public void Dispose() { if (!Active) return; Active = false; remove(this); }
    }
}
