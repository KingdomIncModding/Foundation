using System;

namespace KingdomFoundation.Core
{
    internal sealed class ModContext : IModContext
    {
        readonly CoreDiagnostics diagnostics;
        readonly ModScope scope;
        public string ModId { get; }
        public IModServices Services { get; }
        public IModContent Content { get; }
        public IModDispatcher Dispatcher { get; }
        public IModEvents Events { get; }
        public IModPatches Patches { get; }
        public ICoreDiagnostics Diagnostics => diagnostics;
        internal ModContext(string modId, CoreServiceRegistry services, CoreEventBus events,
            CorePatchManager patches, CoreContentCatalog catalog, CoreDispatcher dispatcher, CoreDiagnostics diagnostics, ModScope scope)
        {
            ModId = modId;
            this.diagnostics = diagnostics;
            this.scope = scope;
            Services = new OwnedServices(modId, services, scope);
            Content = new OwnedContent(modId, catalog, scope);
            Dispatcher = new OwnedDispatcher(modId, dispatcher, scope);
            Events = new OwnedEvents(modId, events, scope);
            Patches = new OwnedPatches(modId, patches, scope);
        }
        public IDisposable Own(IDisposable item) => scope.Track(item);
        public void Log(CoreSeverity severity, string operation, string message, Exception error = null)
            => diagnostics.Record(severity, ModId, operation, message, error);
    }
}
