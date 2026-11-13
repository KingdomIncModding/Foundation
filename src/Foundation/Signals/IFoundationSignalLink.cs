using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>A live Foundation signal-to-signal binding. Never a native item pipe.</summary>
    public interface IFoundationSignalLink : IDisposable
    {
        string OwnerId { get; }

        SignalSelection Source { get; }

        SignalSelection Destination { get; }

        bool IsResolved { get; }

        string UnavailableReason { get; }
    }
}
