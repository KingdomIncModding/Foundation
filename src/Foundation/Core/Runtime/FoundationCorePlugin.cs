using BepInEx;

namespace KingdomFoundation.Core
{
    /// <summary>
    /// Companion BepInEx component in the SAME Foundation.dll. Existing Foundation
    /// plugin classes do not need editing; both plugin types are discovered by BepInEx.
    /// </summary>
    [BepInPlugin(Id, "Foundation Core", FoundationPlugin.Version)]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FoundationCorePlugin : BaseUnityPlugin
    {
        internal const string Id = "urakka.kingdominc.foundation.core";

        void Awake()
        {
            FoundationCoreApi.Runtime.AttachLogger(Logger);
            FoundationCoreApi.Runtime.AttachMainThread();
            FoundationCoreApi.Runtime.InstallLifecycleHooks();
            Logger.LogInfo("Foundation Core API " + CoreApiVersion.Current + " initialized.");
        }
        void Update() => FoundationCoreApi.Runtime.Tick();
        void OnDestroy() => FoundationCoreApi.Runtime.Shutdown();
        void OnApplicationQuit() => FoundationCoreApi.Runtime.Shutdown();
    }
}
