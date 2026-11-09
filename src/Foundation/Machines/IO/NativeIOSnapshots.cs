using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Immutable facts read from the game's actual FlowIO, not a Foundation buffer.</summary>
    public sealed class NativeIOPortSnapshot
    {
        public string MachineGuid { get; }
        public string MachineType { get; }
        public int FlowIndex { get; }
        public MachinePortDirection Direction { get; }
        public int Index { get; }
        public int NativePosition { get; }
        public string ItemTypeId { get; }
        public int BufferCapacity { get; }
        public int BufferedCount { get; }
        public IReadOnlyList<string> BufferedItemTypeIds { get; }
        public bool CanBuffer { get; }
        public bool IsConnected { get; }
        public bool HasNativeFilter { get; }
        public bool FilterPreviewEnabled { get; }
        /// <summary>True only when native Item is fixed and no custom filter overrides CanAcceptItem.</summary>
        public bool PreviewIsExhaustive { get; }
        public IReadOnlyList<string> NativePreviewItemTypeIds { get; }
        public string ConnectedMachineGuid { get; }
        public int ConnectedPortIndex { get; }

        internal NativeIOPortSnapshot(FlowIO port, int flowIndex)
        {
            if (port == null) throw new ArgumentNullException(nameof(port));
            MachineGuid = port.Parent?.Guid;
            MachineType = port.Parent?.ElementType;
            FlowIndex = flowIndex;
            Direction = port is FlowInput ? MachinePortDirection.Input : MachinePortDirection.Output;
            Index = port.ParentIndex;
            NativePosition = port.Position;
            ItemTypeId = port.Item?.Identifier;
            BufferCapacity = port.BufferSize;
            BufferedCount = port.BufferedItems.Count;
            CanBuffer = port.CanBuffer;
            HasNativeFilter = port.Filter != null;
            FilterPreviewEnabled = port.FilterPreview;
            PreviewIsExhaustive = port.Filter == null && port.Item != null;
            var items = new List<string>(port.BufferedItems.Count);
            foreach (var item in port.BufferedItems) items.Add(item?.Type?.Identifier);
            BufferedItemTypeIds = items.AsReadOnly();
            // IMPORTANT: GetPreviewTypes is not exhaustive for a Filter delegate
            // with FilterPreview=false (Collector, Distributor and Manifold).
            // Do not pretend an empty preview means the port rejects all items.
            var previews = new List<string>();
            if (!port.FilterPreview || port.Filter != null)
                foreach (var itemType in port.GetPreviewTypes())
                    if (itemType != null) previews.Add(itemType.Identifier);
            NativePreviewItemTypeIds = previews.AsReadOnly();
            var pipe = port.ConnectedPipe;
            IsConnected = pipe != null;
            FlowIO peer = port is FlowInput ? (FlowIO)pipe?.ConnectedOutput : pipe?.ConnectedInput;
            ConnectedMachineGuid = peer?.Parent?.Guid;
            ConnectedPortIndex = peer?.ParentIndex ?? -1;
        }
    }

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
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
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

    /// <summary>Native logistical machine routing state; values are read from actual game fields.</summary>
    public sealed class NativeIORouterSnapshot
    {
        public string MachineGuid { get; }
        public string NativeType { get; }
        public int InputCount { get; }
        public int OutputCount { get; }
        public int? CurrentInputIndex { get; }
        public int? CurrentOutputIndex { get; }
        public string LastItemTypeId { get; }
        /// <summary>Four is the observed in-game UI expansion limit, not a transfer rate.</summary>
        public int NativeUiPortLimit { get; }
        internal NativeIORouterSnapshot(FlowElement machine, int? currentInput, int? currentOutput,
            string lastType, int nativeUiPortLimit)
        {
            MachineGuid = machine.Guid;
            NativeType = machine.ElementType;
            InputCount = machine.Inputs.Count;
            OutputCount = machine.Outputs.Count;
            CurrentInputIndex = currentInput;
            CurrentOutputIndex = currentOutput;
            LastItemTypeId = lastType;
            NativeUiPortLimit = nativeUiPortLimit;
        }
    }

    /// <summary>Measured pipe events aggregated by direction. Not a machine capacity.</summary>
    public sealed class NativeIOMachineThroughputSnapshot
    {
        public string MachineGuid { get; }
        public int FlowIndex { get; }
        public long ObservedInputItems { get; }
        public long ObservedOutputItems { get; }
        public double ObservedInputItemsPerSecond { get; }
        public double ObservedOutputItemsPerSecond { get; }
        /// <summary>Native production field when applicable, distinct from actual transfer observations.</summary>
        public float? NativeProductionOutputPerSecond { get; }
        public int ObservationWindowSeconds => 10;
        internal NativeIOMachineThroughputSnapshot(string guid, int flow, long incoming, long outgoing,
            double incomingRate, double outgoingRate, float? production)
        {
            MachineGuid=guid;FlowIndex=flow;ObservedInputItems=incoming;ObservedOutputItems=outgoing;
            ObservedInputItemsPerSecond=incomingRate;ObservedOutputItemsPerSecond=outgoingRate;
            NativeProductionOutputPerSecond=production;
        }
    }

    public sealed class NativeIOValidation
    {
        public bool Allowed { get; }
        public string Reason { get; }
        internal NativeIOValidation(bool allowed, string reason) { Allowed = allowed; Reason = reason; }
        internal static NativeIOValidation Yes() => new NativeIOValidation(true, "Native operation permitted by currently observable port/workspace state.");
        internal static NativeIOValidation No(string message) => new NativeIOValidation(false, message);
    }

    /// <summary>Event after native pipe placement or disconnection (not a ghost preview).</summary>
    public sealed class NativeIOConnectionEvent
    {
        public NativeIOPipeSnapshot Pipe { get; }
        public bool Connected { get; }
        internal NativeIOConnectionEvent(NativeIOPipeSnapshot snapshot, bool connected)
        { Pipe = snapshot; Connected = connected; }
    }
}
