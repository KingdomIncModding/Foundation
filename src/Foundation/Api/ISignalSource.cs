using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the isignal source API.</summary>
    public interface ISignalSource : IDisposable
    {
        string ModId { get; }
        string Id { get; }
        string Name { get; }
        bool IsRegistered { get; }
        IReadOnlyList<ISignalProperty> Properties { get; }
        bool TryGetProperty(string propertyId, out ISignalProperty property);
    }
}
