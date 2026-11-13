using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Immutable reading of a real native FlowIO. Item type is a native registered identifier.</summary>
    public sealed class MachinePortSnapshot
    {
        public MachinePortDirection Direction { get; }
        public int Index { get; }
        public int Position { get; }
        public string ItemTypeId { get; }
        public int BufferCapacity { get; }
        public int BufferCount { get; }
        public bool IsConnected { get; }
        public string ConnectedMachineGuid { get; }
        public int ConnectedPortIndex { get; }
        public bool CanBuffer { get; }
        public bool HasFilter { get; }
        public bool FilterPreview { get; }

        internal MachinePortSnapshot(FlowIO port, MachinePortDirection direction)
        {
            Direction = direction;
            Index = port.ParentIndex;
            Position = port.Position;
            ItemTypeId = port.Item?.Identifier;
            BufferCapacity = port.BufferSize;
            BufferCount = port.BufferedItems.Count;
            CanBuffer = port.CanBuffer;
            HasFilter = port.Filter != null;
            FilterPreview = port.FilterPreview;
            var pipe = port.ConnectedPipe;
            IsConnected = pipe != null;
            var peer = direction == MachinePortDirection.Input ? (FlowIO)pipe?.ConnectedOutput : pipe?.ConnectedInput;
            ConnectedMachineGuid = peer?.Parent?.Guid;
            ConnectedPortIndex = peer?.ParentIndex ?? -1;
        }
    }
}
