using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>One successful item transfer reported by a live native WorldFlowPipe.</summary>
    public sealed class FlowItemTransferSnapshot
    {
        public FlowPipeSnapshot Pipe { get; }
        public string ItemTypeId { get; }
        public float Quality { get; }

        internal FlowItemTransferSnapshot(FlowPipe pipe, InventoryItem item)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            if (item == null) throw new ArgumentNullException(nameof(item));
            Pipe = new FlowPipeSnapshot(pipe);
            ItemTypeId = item.Type?.Identifier;
            Quality = item.Quality;
        }
    }
}
