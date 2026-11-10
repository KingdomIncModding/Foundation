using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace KingdomFoundation.Core
{
    internal sealed class CoreDiagnostics : ICoreDiagnostics
    {
        readonly object gate = new object();
        readonly Queue<CoreDiagnosticRecord> history = new Queue<CoreDiagnosticRecord>();
        ManualLogSource logger;
        public IReadOnlyList<CoreDiagnosticRecord> Recent
        {
            get { lock (gate) return Array.AsReadOnly(history.ToArray()); }
        }
        internal void SetLogger(ManualLogSource target) { logger = target; }
        /// <summary>Performs the record operation.</summary>
        public void Record(CoreSeverity severity, string ownerId, string operation, string message, Exception error = null)
        {
            var record = new CoreDiagnosticRecord(severity, ownerId, operation, message, error);
            lock (gate)
            {
                history.Enqueue(record);
                while (history.Count > 300) history.Dequeue();
            }
            var output = "[" + record.OwnerId + "/" + record.Operation + "] " + record.Message;
            if (error != null) output += "\n" + error;
            var target = logger;
            if (target == null) return;
            if (severity == CoreSeverity.Error) target.LogError(output);
            else if (severity == CoreSeverity.Warning) target.LogWarning(output);
            else target.LogInfo(output);
        }
    }
}
