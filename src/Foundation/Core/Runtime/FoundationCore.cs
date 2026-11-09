using System;
using Assets.Source.Player;
using BepInEx.Logging;

namespace KingdomFoundation.Core
{
    internal sealed class FoundationCore : IFoundationCore
    {
        readonly CoreDiagnostics diagnostics;
        readonly CoreEventBus events;
        readonly CoreServiceRegistry services;
        readonly CoreContentCatalog content;
        readonly CoreDispatcher dispatcher;
        readonly CorePatchManager patches;
        readonly CorePluginCatalog plugins;
        readonly CoreModuleRegistry modules;
        readonly CoreModDirectory modDirectory;
        bool shuttingDown;
        GamePlayer latestWorld;
        public Version ApiVersion => CoreApiVersion.Current;
        public ICoreModules Modules => modules;
        public ICoreServices Services => services;
        public ICoreDispatcher Dispatcher => dispatcher;
        public ICoreContentCatalog Content => content;
        public ICoreEventBus Events => events;
        public ICorePatchManager Patches => patches;
        public ICorePluginCatalog Plugins => plugins;
        public ICoreModDirectory Mods => modDirectory;
        public ICoreServiceDirectory ServiceDirectory => services;
        public ICoreDiagnostics Diagnostics => diagnostics;
        internal FoundationCore()
        {
            diagnostics = new CoreDiagnostics();
            events = new CoreEventBus(diagnostics);
            services = new CoreServiceRegistry(diagnostics, events);
            content = new CoreContentCatalog(events);
            dispatcher = new CoreDispatcher(diagnostics);
            patches = new CorePatchManager(diagnostics);
            plugins = new CorePluginCatalog();
            modules = new CoreModuleRegistry(diagnostics, services, events, patches, content, dispatcher, plugins);
            modDirectory = new CoreModDirectory(modules, plugins);
            services.SetWorldAvailability(() => latestWorld != null);
            content.SetWorldAvailability(() => latestWorld != null);
        }
        internal void AttachLogger(ManualLogSource source) => diagnostics.SetLogger(source);
        internal void AttachMainThread() => dispatcher.AttachMainThread();
        internal void InstallLifecycleHooks() => new CoreLifecycleInstaller(diagnostics, patches).Install();
        internal void Tick()
        {
            if (shuttingDown) return;
            try
            {
                dispatcher.Drain();
                latestWorld = GamePlayer.Current;
                modules.Tick(latestWorld);
            }
            catch (Exception error)
            {
                diagnostics.Record(CoreSeverity.Error, "foundation", "update", "Core update failed.", error);
            }
        }
        internal void Publish<T>(T evt) => events.Publish(evt);
        internal void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            try { modules.StopAll(); }
            finally
            {
                dispatcher.Stop();
                services.ReleaseAll();
                events.ReleaseAll();
                patches.ReleaseAll();
            }
            diagnostics.Record(CoreSeverity.Info, "foundation", "shutdown", "Core integration stopped.");
        }
    }
}
