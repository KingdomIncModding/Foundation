using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the imod context API.</summary>
    public interface IModContext
    {
        string ModId { get; }
        IModServices Services { get; }
        IModContent Content { get; }
        IModDispatcher Dispatcher { get; }
        IModEvents Events { get; }
        IModPatches Patches { get; }
        /// <summary>Gets or sets the diagnostics value.</summary>
        ICoreDiagnostics Diagnostics { get; }
        IDisposable Own(IDisposable resource);
        void Log(CoreSeverity severity, string operation, string message, Exception error = null);
    }
}
