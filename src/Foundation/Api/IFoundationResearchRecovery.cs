using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    /// <summary>Read-only view of unknown native tech IDs preserved across saves.
    /// No research unlocks, fake technology definitions or custom simulation.</summary>
    public interface IFoundationResearchRecovery
    {
        IReadOnlyList<string> RetainedTechIds { get; }
        IReadOnlyList<string> RetainedNotificationIds { get; }
    }
    internal sealed class FoundationResearchRecovery : IFoundationResearchRecovery
    {
        public IReadOnlyList<string> RetainedTechIds => MissingResearchRecovery.RetainedIds;
        public IReadOnlyList<string> RetainedNotificationIds => MissingResearchRecovery.RetainedNotificationIds;
    }
}
