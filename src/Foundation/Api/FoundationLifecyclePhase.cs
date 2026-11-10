using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the foundation lifecycle phase API.</summary>
    public enum FoundationLifecyclePhase
    {
        NewKingdom,
        SaveLoaded,
        GameReady,
        BeforeSave,
        GameLeaving
    }
}
