using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation lifecycle service contract.</summary>
    public interface IFoundationLifecycleService
    {
        FoundationLifecyclePhase? LastPhase { get; }
        event Action<FoundationLifecycleEvent> Changed;
    }
}
