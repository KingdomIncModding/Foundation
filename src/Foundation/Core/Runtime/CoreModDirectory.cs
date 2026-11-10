using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    /// <summary>No additional registry or cached authority: both catalogs are read live.</summary>
    internal sealed class CoreModDirectory : ICoreModDirectory
    {
        readonly ICoreModules modules;
        readonly ICorePluginCatalog plugins;
        internal CoreModDirectory(ICoreModules modules, ICorePluginCatalog plugins)
        { this.modules = modules; this.plugins = plugins; }

        public IReadOnlyList<ModDirectoryEntry> Entries
        {
            get
            {
                var unique = new Dictionary<string, ModDirectoryEntry>(StringComparer.Ordinal);
                foreach (var plugin in plugins.Plugins)
                    unique[plugin.Id] = new ModDirectoryEntry(plugin.Id, plugin, null);
                foreach (var module in modules.Registered)
                {
                    unique.TryGetValue(module.Descriptor.Id, out var current);
                    unique[module.Descriptor.Id] = new ModDirectoryEntry(module.Descriptor.Id, current?.Plugin, module);
                }
                return Array.AsReadOnly(unique.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
            }
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string modId, out ModDirectoryEntry entry)
        {
            entry = string.IsNullOrWhiteSpace(modId) ? null
                : Entries.FirstOrDefault(x => string.Equals(x.Id, modId, StringComparison.Ordinal));
            return entry != null;
        }
    }
}
