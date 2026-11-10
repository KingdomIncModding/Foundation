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
        /// <summary>Connects native output and input ports, optionally specifying pipe route nodes.</summary>
        /// <param name="output">The output value.</param>
        /// <param name="input">The input value.</param>
        /// <param name="route">The route value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        FlowPipe Connect(FlowOutput output, FlowInput input, IEnumerable<Vector2Int> route = null);
        /// <summary>Disconnects the specified native pipe.</summary>
        /// <param name="pipe">The pipe value.</param>
        void Disconnect(FlowPipe pipe);
        /// <summary>Checks whether the native flow port has a connection.</summary>
        /// <param name="port">The port value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool IsConnected(FlowIO port);
        /// <summary>Returns measured throughput information for the specified native pipe.</summary>
        /// <param name="pipe">The pipe value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        FlowPipeMetrics GetMeasuredThroughput(FlowPipe pipe);
        /// <summary>Returns native pipes attached to the specified flow element.</summary>
        /// <param name="element">The element value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        IReadOnlyList<FlowPipe> FindPipes(FlowElement element);
        /// <summary>Replaces the pipe's bend-node route and updates its native representation.</summary>
        /// <param name="pipe">The pipe value.</param>
        /// <param name="nodes">The nodes value.</param>
        void ReplaceRoute(FlowPipe pipe, IEnumerable<Vector2Int> nodes);
        /// <summary>Inserts a bend node at the specified route index.</summary>
        /// <param name="pipe">The pipe value.</param>
        /// <param name="index">The index value.</param>
        /// <param name="position">The position value.</param>
        void InsertRouteNode(FlowPipe pipe, int index, Vector2Int position);
        /// <summary>Removes a bend node at the specified route index.</summary>
        /// <param name="pipe">The pipe value.</param>
        /// <param name="index">The index value.</param>
        void RemoveRouteNode(FlowPipe pipe, int index);
    }
}
