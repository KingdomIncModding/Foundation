using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public interface IFoundationLifecycleService
    {
        FoundationLifecyclePhase? LastPhase { get; }
        event Action<FoundationLifecycleEvent> Changed;
    }
}
