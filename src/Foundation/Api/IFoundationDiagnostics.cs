using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation diagnostics contract.</summary>
    public interface IFoundationDiagnostics
    {
        IReadOnlyList<FoundationDiagnosticEntry> Recent { get; }
        void Write(string modId, FoundationDiagnosticLevel level, string message);
    }
}
