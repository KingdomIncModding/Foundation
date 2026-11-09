using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    public interface ICoreDiagnostics
    {
        IReadOnlyList<CoreDiagnosticRecord> Recent { get; }
        void Record(CoreSeverity severity, string ownerId, string operation, string message, Exception error = null);
    }
}
