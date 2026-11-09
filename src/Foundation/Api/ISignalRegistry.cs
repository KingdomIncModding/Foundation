using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>A mod-defined source of individually discoverable named properties.
    /// This is an API for exposed values; it does not replace native item pipes.</summary>
    public interface ISignalRegistry
    {
        IReadOnlyList<ISignalSource> Sources { get; }
        event Action Changed;
        ISignalSource Register(SignalSourceDefinition definition);
        bool TryGet(string sourceId, out ISignalSource source);
        int RemoveOwner(string modId);
    }
}
