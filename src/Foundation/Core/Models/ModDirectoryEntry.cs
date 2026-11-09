using System;

namespace KingdomFoundation.Core
{
    /// <summary>A single mod ID's live Foundation registration and/or BepInEx metadata.</summary>
    public sealed class ModDirectoryEntry
    {
        public string Id { get; }
        public string Name { get; }
        public Version Version { get; }
        public PluginSnapshot Plugin { get; }
        public ModSnapshot Module { get; }
        public bool IsPluginLoaded => Plugin != null;
        public bool IsModuleRegistered => Module != null;
        public ModDirectoryEntry(string id, PluginSnapshot plugin, ModSnapshot module)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Plugin = plugin;
            Module = module;
            Name = module?.Descriptor.Name ?? plugin?.Name ?? id;
            Version = module?.Descriptor.Version ?? plugin?.Version;
        }
    }
}
