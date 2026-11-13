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
}
