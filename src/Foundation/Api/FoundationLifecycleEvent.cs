using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the foundation lifecycle event API.</summary>
    public sealed class FoundationLifecycleEvent
    {
        public FoundationLifecyclePhase Phase { get; }
        public GamePlayer Player { get; }
        public FoundationLifecycleEvent(FoundationLifecyclePhase phase, GamePlayer player) { Phase = phase; Player = player; }
    }
}
