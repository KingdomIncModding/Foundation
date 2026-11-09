using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    public interface ICorePluginCatalog
    {
        IReadOnlyList<PluginSnapshot> Plugins { get; }
        bool TryGet(string id, out PluginSnapshot plugin);
        bool Has(string id, Version minimumVersion = null);
    }
}
