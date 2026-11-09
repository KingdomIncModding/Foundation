using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class FoundationLifecycleEvent
    {
        public FoundationLifecyclePhase Phase { get; }
        public GamePlayer Player { get; }
        public FoundationLifecycleEvent(FoundationLifecyclePhase phase, GamePlayer player) { Phase = phase; Player = player; }
    }
}
