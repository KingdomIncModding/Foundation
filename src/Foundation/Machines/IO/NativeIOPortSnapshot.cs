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
            if (port == null)
                throw new ArgumentNullException(nameof(port));
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
            foreach (var item in port.BufferedItems)
                items.Add(item?.Type?.Identifier);
            BufferedItemTypeIds = items.AsReadOnly();
            // IMPORTANT: GetPreviewTypes is not exhaustive for a Filter delegate
            // with FilterPreview=false (Collector, Distributor and Manifold).
            // Do not pretend an empty preview means the port rejects all items.
            var previews = new List<string>();
            if (!port.FilterPreview || port.Filter != null)
                foreach (var itemType in port.GetPreviewTypes())
                    if (itemType != null)
                        previews.Add(itemType.Identifier);
            NativePreviewItemTypeIds = previews.AsReadOnly();
            var pipe = port.ConnectedPipe;
            IsConnected = pipe != null;
            FlowIO peer = port is FlowInput ? (FlowIO)pipe?.ConnectedOutput : pipe?.ConnectedInput;
            ConnectedMachineGuid = peer?.Parent?.Guid;
            ConnectedPortIndex = peer?.ParentIndex ?? -1;
        }
    }
}
