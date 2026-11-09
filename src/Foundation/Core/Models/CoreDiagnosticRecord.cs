using System;

namespace KingdomFoundation.Core
{
    public sealed class CoreDiagnosticRecord
    {
        public DateTime TimestampUtc { get; }
        public CoreSeverity Severity { get; }
        public string OwnerId { get; }
        public string Operation { get; }
        public string Message { get; }
        public string ExceptionText { get; }
        public CoreDiagnosticRecord(CoreSeverity severity, string ownerId, string operation,
            string message, Exception exception = null)
        {
            TimestampUtc = DateTime.UtcNow;
            Severity = severity;
            OwnerId = ownerId ?? "foundation";
            Operation = operation ?? "unknown";
            Message = message ?? string.Empty;
            ExceptionText = exception?.ToString();
        }
    }
}
