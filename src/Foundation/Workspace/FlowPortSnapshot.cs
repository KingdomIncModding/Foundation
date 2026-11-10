using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Immutable view of one native input/output port at capture time.</summary>
    public sealed class FlowPortSnapshot
    {
        public string ElementGuid { get; }
        public string ElementType { get; }
        public FlowPortDirection Direction { get; }
        public int Index { get; }
        public int Position { get; }
        public string AcceptedItemTypeId { get; }
        /// <summary>Gets or sets the has custom filter value.</summary>
        public bool HasCustomFilter { get; }
        public bool FilterPreview { get; }
        public int BufferCapacity { get; }
        public int BufferedCount { get; }
        public IReadOnlyList<string> BufferedItemTypeIds { get; }
        public bool IsConnected { get; }
        public bool CanBuffer => BufferedCount < BufferCapacity;

        internal FlowPortSnapshot(FlowIO port)
        {
            if (port == null) throw new ArgumentNullException(nameof(port));
            ElementGuid = port.Parent?.Guid;
            ElementType = port.Parent?.ElementType;
            Direction = port is FlowInput ? FlowPortDirection.Input : FlowPortDirection.Output;
            Index = port.ParentIndex;
            Position = port.Position;
            AcceptedItemTypeId = port.Item?.Identifier;
            HasCustomFilter = port.Filter != null;
            FilterPreview = port.FilterPreview;
            BufferCapacity = port.BufferSize;
            BufferedCount = port.BufferedItems.Count;
            var types = new List<string>(BufferedCount);
            foreach (var item in port.BufferedItems) types.Add(item?.Type?.Identifier);
            BufferedItemTypeIds = types.AsReadOnly();
            IsConnected = port.ConnectedPipe != null;
        }
    }
}
