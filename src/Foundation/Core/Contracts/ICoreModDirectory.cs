using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>
    /// Single live discovery view over registered Foundation modules and the BepInEx
    /// chainloader. It does not own or duplicate either provider's registrations.
    /// </summary>
    public interface ICoreModDirectory
    {
        IReadOnlyList<ModDirectoryEntry> Entries { get; }
        bool TryGet(string modId, out ModDirectoryEntry entry);
    }
}
