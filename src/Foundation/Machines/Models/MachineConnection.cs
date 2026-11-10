using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;
namespace KingdomFoundation.Machines
{
    /// <summary>Native pipe endpoints and path; observed rate counts actual native ItemTransferred calls.</summary>
    public sealed class MachineConnection
    {
        public string SourceGuid { get; }
        public int SourceOutput { get; }
        public string DestinationGuid { get; }
        public int DestinationInput { get; }
        public IReadOnlyList<Vector2Int> Path { get; }
        public bool LastItemRejected { get; }
        /// <summary>Gets or sets the observed items value.</summary>
        public long ObservedItems { get; }
        public double ObservedItemsPerSecond { get; }
        internal MachineConnection(FlowPipe pipe, long count, double rate)
        {
            SourceGuid = pipe.ConnectedOutput?.Parent?.Guid;
            SourceOutput = pipe.ConnectedOutput?.ParentIndex ?? -1;
            DestinationGuid = pipe.ConnectedInput?.Parent?.Guid;
            DestinationInput = pipe.ConnectedInput?.ParentIndex ?? -1;
            Path = new List<Vector2Int>(pipe.Nodes).AsReadOnly();
            LastItemRejected = pipe.ItemRejected;
            ObservedItems = count;
            ObservedItemsPerSecond = rate;
        }
    }

    /// <summary>Provides the machine operation result API.</summary>
    public sealed class MachineOperationResult
    {
        public bool Succeeded { get; }
        public string Message { get; }
        private MachineOperationResult(bool success, string message) { Succeeded = success; Message = message; }
        public static MachineOperationResult Ok(string message = "Native operation completed") => new MachineOperationResult(true, message);
        public static MachineOperationResult Reject(string message) => new MachineOperationResult(false, message);
        public override string ToString() => (Succeeded ? "OK: " : "REJECTED: ") + Message;
    }
}
