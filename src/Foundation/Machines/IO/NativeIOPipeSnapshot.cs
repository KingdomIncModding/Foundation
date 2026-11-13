using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Immutable native pipe topology with an explicitly OBSERVED, not rated, throughput.</summary>
    public sealed class NativeIOPipeSnapshot
    {
        public int FlowIndex { get; }
        public string SourceGuid { get; }
        public int SourceOutputIndex { get; }
        public string DestinationGuid { get; }
        public int DestinationInputIndex { get; }
        public IReadOnlyList<Vector2Int> Route { get; }
        /// <summary>Transient native FlowPipe.ItemRejected flag, reset each native Update.</summary>
        public bool LastUpdateRejectedItem { get; }
        /// <summary>Count of actual observed WorldFlowPipe.ItemTransferred calls since observation began.</summary>
        public long ObservedTransfers { get; }
        /// <summary>Observed events per real-time second over a trailing 10-second window; NOT a pipe capacity.</summary>
        public double ObservedTransfersPerSecond { get; }
        public int ObservationWindowSeconds => 10;
        public bool EverObservedTransfer => ObservedTransfers > 0;

        internal NativeIOPipeSnapshot(FlowPipe pipe, int flowIndex, long total, double rate)
        {
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));
            FlowIndex = flowIndex;
            SourceGuid = pipe.ConnectedOutput?.Parent?.Guid;
            SourceOutputIndex = pipe.ConnectedOutput?.ParentIndex ?? -1;
            DestinationGuid = pipe.ConnectedInput?.Parent?.Guid;
            DestinationInputIndex = pipe.ConnectedInput?.ParentIndex ?? -1;
            Route = new List<Vector2Int>(pipe.Nodes).AsReadOnly();
            LastUpdateRejectedItem = pipe.ItemRejected;
            ObservedTransfers = total;
            ObservedTransfersPerSecond = rate;
        }
    }
}
