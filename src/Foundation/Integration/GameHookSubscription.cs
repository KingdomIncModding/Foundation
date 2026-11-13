using System;

namespace KingdomFoundation.Api
{
    internal sealed class GameHookSubscription : IDisposable
    {
        internal string ModId;
        internal string HookId;
        internal Action<GameHookCall> Before;
        internal Action<GameHookCall> After;
        internal int Priority;
        internal bool Active = true;
        internal GameHookFilter Filter;
        private readonly GameHookRegistry owner;
        internal GameHookSubscription(GameHookRegistry owner) { this.owner = owner; }
        public void Dispose() { if (!Active) return; Active = false; owner.Remove(this); }
    }
}
