using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Core;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationEventBusEntry : IDisposable
    {
        internal FoundationEventBus Owner;
        internal string ModId;
        internal Type Type;
        internal Delegate Callback;
        internal IDisposable Token;
        internal bool Active = true;
        internal bool IsActive => Active && (!(Token is ICoreEventSubscription subscription) || subscription.IsActive);

        public void Dispose() => Owner.Unsubscribe(this);
    }
}
