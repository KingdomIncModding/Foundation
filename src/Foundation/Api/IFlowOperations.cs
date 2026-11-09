using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Opt-in native item-pipe operations on the active game workspace.</summary>
    public interface IFlowOperations
    {
        FlowPipe Connect(FlowOutput output, FlowInput input, IEnumerable<Vector2Int> route = null);
        void Disconnect(FlowPipe pipe);
        bool IsConnected(FlowIO port);
        FlowPipeMetrics GetMeasuredThroughput(FlowPipe pipe);
        /// <summary>All live native pipes connected to an element, across the loaded kingdom.</summary>
        IReadOnlyList<FlowPipe> FindPipes(FlowElement element);
        /// <summary>Replace bend-node positions, updating native line/collider visuals.</summary>
        void ReplaceRoute(FlowPipe pipe, IEnumerable<Vector2Int> nodes);
        void InsertRouteNode(FlowPipe pipe, int index, Vector2Int position);
        void RemoveRouteNode(FlowPipe pipe, int index);
    }
}
