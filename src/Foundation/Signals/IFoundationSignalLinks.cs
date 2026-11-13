using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>Owner-scoped routing over registered Foundation signal properties.</summary>
    public interface IFoundationSignalLinks
    {
        IReadOnlyList<IFoundationSignalLink> Links { get; }

        IFoundationSignalLink Connect(string ownerId, SignalSelection source, SignalSelection destination);
        int RemoveOwner(string ownerId);
        /// <summary>Serializable selections; save these inside the mod's existing ModSave payload.</summary>
        JsonValue ExportOwner(string ownerId);
        /// <summary>Restores selection IDs without writing any game state.</summary>
        IReadOnlyList<IFoundationSignalLink> ImportOwner(string ownerId, JsonValue stored);
    }
}
