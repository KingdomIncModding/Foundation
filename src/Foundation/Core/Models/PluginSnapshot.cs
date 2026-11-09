using System;

namespace KingdomFoundation.Core
{
    public sealed class PluginSnapshot
    {
        public string Id { get; }
        public string Name { get; }
        public Version Version { get; }
        public PluginSnapshot(string id, string name, Version version)
        { Id = id; Name = name; Version = version; }
    }
}
