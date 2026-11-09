using System;
using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    /// <summary>Additive owner lookup over the existing native content registry.</summary>
    public interface IGameContentDirectory : IGameContentRegistry
    {
        bool TryGetOwner(string kind, string nativeId, out string ownerId);
        IReadOnlyList<string> OwnedIds(string modId);
    }
}
