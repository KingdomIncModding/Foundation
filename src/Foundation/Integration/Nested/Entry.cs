using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Core;

namespace KingdomFoundation.Api
{
    internal sealed partial class FoundationEventBus
    {
        sealed class Entry : IDisposable
        {
            internal FoundationEventBus Owner;
            internal string ModId;
            internal Type Type;
            internal Delegate Callback;
            internal IDisposable Token;
            internal bool Active = true;
            internal bool IsActive => Active && (!(Token is ICoreEventSubscription subscription) || subscription.IsActive);
            /// <summary>Performs the dispose operation.</summary>
            public void Dispose() => Owner.Unsubscribe(this);
        }
    }

}
