using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;

namespace KingdomFoundation.Core
{
    internal sealed class CorePluginCatalog : ICorePluginCatalog
    {
        public IReadOnlyList<PluginSnapshot> Plugins
        {
            get
            {
                return Array.AsReadOnly(Chainloader.PluginInfos.Values
                    .Where(x => x != null && x.Metadata != null)
                    .Select(x => new PluginSnapshot(x.Metadata.GUID, x.Metadata.Name, x.Metadata.Version))
                    .OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
            }
        }
        public bool TryGet(string id, out PluginSnapshot plugin)
        {
            plugin = Plugins.FirstOrDefault(x => x.Id == id);
            return plugin != null;
        }
        public bool Has(string id, Version minimumVersion = null)
            => TryGet(id, out var plugin) && (minimumVersion == null || plugin.Version >= minimumVersion);
    }
}
