using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationResearchRecovery : IFoundationResearchRecovery
    {
        public IReadOnlyList<string> RetainedTechIds => MissingResearchRecovery.RetainedIds;
        public IReadOnlyList<string> RetainedNotificationIds => MissingResearchRecovery.RetainedNotificationIds;
    }
}
