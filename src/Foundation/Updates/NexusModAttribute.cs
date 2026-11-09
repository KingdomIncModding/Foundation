using System;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Declares the Nexus Mods mod ID associated with this plugin for automatic version checks in Foundation's Installed menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public sealed class NexusModAttribute : Attribute
    {
        /// <summary>The numerical Nexus mod ID for the game (e.g. 18 for https://www.nexusmods.com/kingdominc/mods/18).</summary>
        public int ModId { get; }

        public NexusModAttribute(int modId)
        {
            ModId = modId;
        }
    }
}

