using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    public sealed class FoundationEventSubscription
    {
        public string ModId { get; }
        public Type EventType { get; }
        internal FoundationEventSubscription(string modId, Type type) { ModId = modId; EventType = type; }
    }
}
