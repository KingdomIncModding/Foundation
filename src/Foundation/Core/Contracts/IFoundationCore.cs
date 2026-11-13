using System;

namespace KingdomFoundation.Core
{
    public interface IFoundationCore
    {
        Version ApiVersion { get; }
        ICoreModules Modules { get; }
        ICoreServices Services { get; }
        ICoreDispatcher Dispatcher { get; }
        ICoreContentCatalog Content { get; }
        ICoreEventBus Events { get; }
        ICorePatchManager Patches { get; }
        ICorePluginCatalog Plugins { get; }
        /// <summary>Unified live discovery of BepInEx plugins and Foundation modules.</summary>
        ICoreModDirectory Mods { get; }
        /// <summary>Enumerate and resolve cross-mod services without assuming their service IDs.</summary>
        ICoreServiceDirectory ServiceDirectory { get; }
        ICoreDiagnostics Diagnostics { get; }
    }
}
