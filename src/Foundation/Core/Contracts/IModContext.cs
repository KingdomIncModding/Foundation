using System;

namespace KingdomFoundation.Core
{
    public interface IModContext
    {
        string ModId { get; }
        IModServices Services { get; }
        IModContent Content { get; }
        IModDispatcher Dispatcher { get; }
        IModEvents Events { get; }
        IModPatches Patches { get; }
        ICoreDiagnostics Diagnostics { get; }
        IDisposable Own(IDisposable resource);
        void Log(CoreSeverity severity, string operation, string message, Exception error = null);
    }
}
