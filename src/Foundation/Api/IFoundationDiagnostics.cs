using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation diagnostics contract.</summary>
    public interface IFoundationDiagnostics
    {
        /// <summary>Returns recent diagnostic entries.</summary>
        IReadOnlyList<FoundationDiagnosticEntry> Recent { get; }
        /// <summary>Records a diagnostic entry associated with the specified mod.</summary>
        /// <param name="modId">The mod id value.</param>
        /// <param name="level">The level value.</param>
        /// <param name="message">The message value.</param>
        void Write(string modId, FoundationDiagnosticLevel level, string message);
    }
}
