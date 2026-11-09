using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Immutable snapshot of the endpoints and route of a native item pipe.</summary>
    public sealed class FlowPipeSnapshot
    {
        public string OutputElementGuid { get; }
        public int OutputPortIndex { get; }
        public string InputElementGuid { get; }
        public int InputPortIndex { get; }
        public bool IsItemRejected { get; }
        public bool IsConnected { get; }
        public IReadOnlyList<Vector2Int> RouteNodes { get; }

        internal FlowPipeSnapshot(FlowPipe pipe)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            OutputElementGuid = pipe.ConnectedOutput?.Parent?.Guid;
            OutputPortIndex = pipe.ConnectedOutput?.ParentIndex ?? -1;
            InputElementGuid = pipe.ConnectedInput?.Parent?.Guid;
            InputPortIndex = pipe.ConnectedInput?.ParentIndex ?? -1;
            IsItemRejected = pipe.ItemRejected;
            IsConnected = pipe.ConnectedInput != null && pipe.ConnectedOutput != null;
            RouteNodes = new List<Vector2Int>(pipe.Nodes).AsReadOnly();
        }
    }
}
