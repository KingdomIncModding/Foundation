using System;
using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    /// <summary>Additive live signal discovery API; ISignalRegistry remains binary compatible.</summary>
    public interface ISignalDirectory : ISignalRegistry
    {
        IReadOnlyList<ISignalSource> Find(string query = null, string ownerId = null);
        bool TryResolve(SignalSelection selection, out ISignalProperty property);
        IReadOnlyList<ISignalProperty> FindProperties(string sourceId, Type valueType = null, bool writable = false);
    }
}
