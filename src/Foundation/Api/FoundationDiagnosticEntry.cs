using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the foundation diagnostic entry API.</summary>
    public sealed class FoundationDiagnosticEntry
    {
        public DateTime TimestampUtc { get; }
        public string Owner { get; }
        public FoundationDiagnosticLevel Level { get; }
        public string Message { get; }
        public FoundationDiagnosticEntry(DateTime timestampUtc, string owner, FoundationDiagnosticLevel level, string message)
        { TimestampUtc = timestampUtc; Owner = owner; Level = level; Message = message; }
    }
}
