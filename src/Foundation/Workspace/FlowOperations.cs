using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed partial class FlowOperations : IFlowOperations
    {

        readonly ConditionalWeakTable<FlowPipe, Counter> meters = new ConditionalWeakTable<FlowPipe, Counter>();
        /// <summary>Determines whether connected.</summary>
        public bool IsConnected(FlowIO port) => port?.ConnectedPipe != null;
        public FlowPipe Connect(FlowOutput output, FlowInput input, IEnumerable<Vector2Int> route = null)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output.ConnectedPipe != null || input.ConnectedPipe != null)
                throw new InvalidOperationException("Both ports must be disconnected before connecting a pipe.");
            if (ReferenceEquals(output.Parent, input.Parent))
                throw new InvalidOperationException("Cannot connect an element to itself using this helper.");
            var manager = WorldFlowManager.Instance;
            if (!manager) throw new InvalidOperationException("Open an active kingdom workspace first.");
            var diagram = manager.CurrentFlow;
            if (!diagram.Elements.Contains(output.Parent) || !diagram.Elements.Contains(input.Parent))
                throw new InvalidOperationException("Both ports must belong to the active flow diagram.");
            var pipe = new FlowPipe();
            if (route != null) pipe.Nodes.AddRange(route);
            pipe.Connect(output, input);
            try { manager.CreateNewPipe(pipe); }
            catch
            {
                // Native manager had not completed placement; do not strand port references.
                output.ConnectedPipe = null; input.ConnectedPipe = null;
                diagram.Pipes.Remove(pipe);
                throw;
            }
            return pipe;
        }
        /// <summary>Performs the disconnect operation.</summary>
        public void Disconnect(FlowPipe pipe)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            var manager = WorldFlowManager.Instance;
            if (!manager) throw new InvalidOperationException("Open an active kingdom workspace first.");
            var player = Assets.Source.Player.GamePlayer.Current;
            if (player == null) throw new InvalidOperationException("No kingdom is active.");
            foreach (var diagram in player.Flows)
                if (diagram.Pipes.Contains(pipe)) { manager.DisconnectPipe(pipe, diagram); return; }
            throw new InvalidOperationException("Pipe is not present in any loaded diagram.");
        }
        /// <summary>Finds pipes.</summary>
        public IReadOnlyList<FlowPipe> FindPipes(FlowElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var player = Assets.Source.Player.GamePlayer.Current;
            if (player == null) return Array.Empty<FlowPipe>();
            var matches = new List<FlowPipe>();
            foreach (var diagram in player.Flows)
                foreach (var pipe in diagram.Pipes)
                    if (pipe != null && (ReferenceEquals(pipe.ConnectedInput?.Parent, element) ||
                        ReferenceEquals(pipe.ConnectedOutput?.Parent, element))) matches.Add(pipe);
            return matches.AsReadOnly();
        }
        static void RequireLivePipe(FlowPipe pipe)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            var player = Assets.Source.Player.GamePlayer.Current;
            if (player == null || !player.Flows.Any(flow => flow.Pipes.Contains(pipe)))
                throw new InvalidOperationException("Pipe must belong to a loaded kingdom workspace.");
        }
        /// <summary>Performs the replace route operation.</summary>
        public void ReplaceRoute(FlowPipe pipe, IEnumerable<Vector2Int> nodes)
        {
            RequireLivePipe(pipe);
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            var route = nodes.ToArray();
            // Preserve the existing route if native visuals refuse an update.
            var old = pipe.Nodes.ToArray();
            pipe.Nodes.Clear();
            pipe.Nodes.AddRange(route);
            try { if (pipe.World) pipe.World.UpdateLinePositions(); }
            catch
            {
                pipe.Nodes.Clear();
                pipe.Nodes.AddRange(old);
                if (pipe.World) pipe.World.UpdateLinePositions();
                throw;
            }
        }
        /// <summary>Performs the insert route node operation.</summary>
        public void InsertRouteNode(FlowPipe pipe, int index, Vector2Int position)
        {
            RequireLivePipe(pipe);
            if (index < 0 || index > pipe.Nodes.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var route = pipe.Nodes.ToList();
            route.Insert(index, position);
            ReplaceRoute(pipe, route);
        }
        /// <summary>Removes route node.</summary>
        public void RemoveRouteNode(FlowPipe pipe, int index)
        {
            RequireLivePipe(pipe);
            if (index < 0 || index >= pipe.Nodes.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var route = pipe.Nodes.ToList();
            route.RemoveAt(index);
            ReplaceRoute(pipe, route);
        }

        internal void Record(FlowPipe pipe)
        {
            if (pipe == null) return;
            var counter = meters.GetOrCreateValue(pipe);
            var second = Mathf.FloorToInt(Time.realtimeSinceStartup);
            var bucket = (second % 10 + 10) % 10;
            if (counter.BucketSeconds[bucket] != second)
            { counter.BucketSeconds[bucket] = second; counter.BucketCounts[bucket] = 0; }
            counter.BucketCounts[bucket]++;
            counter.Total++;
        }
        /// <summary>Gets measured throughput.</summary>
        public FlowPipeMetrics GetMeasuredThroughput(FlowPipe pipe)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            if (!meters.TryGetValue(pipe, out var counter)) return new FlowPipeMetrics(0, 0);
            var second = Mathf.FloorToInt(Time.realtimeSinceStartup);
            int recent = 0;
            for (int i = 0; i < 10; i++)
                if (counter.BucketSeconds[i] <= second && second - counter.BucketSeconds[i] < 10)
                    recent += counter.BucketCounts[i];
            return new FlowPipeMetrics(counter.Total, recent);
        }
    }
}
