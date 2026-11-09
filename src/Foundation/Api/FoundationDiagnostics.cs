using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationDiagnostics : IFoundationDiagnostics
    {
        const int MaxHistory = 200;
        readonly Queue<FoundationDiagnosticEntry> recent = new Queue<FoundationDiagnosticEntry>();
        readonly object gate = new object();
        public IReadOnlyList<FoundationDiagnosticEntry> Recent
        { get { lock (gate) return Array.AsReadOnly(recent.ToArray()); } }
        public void Write(string modId, FoundationDiagnosticLevel level, string message)
        {
            var item = new FoundationDiagnosticEntry(DateTime.UtcNow, string.IsNullOrEmpty(modId) ? "Foundation" : modId, level, message ?? "");
            lock (gate)
            {
                recent.Enqueue(item);
                if (recent.Count > MaxHistory) recent.Dequeue();
            }
        }
    }
}
