using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public enum FoundationLifecyclePhase
    {
        NewKingdom,
        SaveLoaded,
        GameReady,
        BeforeSave,
        GameLeaving
    }
}
