using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore plugin catalog API.</summary>
    public interface ICorePluginCatalog
    {
        IReadOnlyList<PluginSnapshot> Plugins { get; }
        bool TryGet(string id, out PluginSnapshot plugin);
        bool Has(string id, Version minimumVersion = null);
    }
}
