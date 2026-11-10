using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation lifecycle service contract.</summary>
    public interface IFoundationLifecycleService
    {
        /// <summary>Gets the most recently observed Foundation lifecycle phase, if any.</summary>
        FoundationLifecyclePhase? LastPhase { get; }
        /// <summary>Raised when Foundation reports a lifecycle transition.</summary>
        event Action<FoundationLifecycleEvent> Changed;
    }
}
