using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>
    /// Strict Game API bridge over Kingdom Inc's own FlowIO, FlowPipe and FlowDiagram.
    /// No invented transfer limits, filters, item routing simulation or duplicated pipe graph.
    /// Call on the Unity main thread. Snapshot values are captured, not continuously updated.
    /// </summary>
    public sealed class MachineNativeIO
    {
        private readonly List<Action<NativeIOConnectionEvent>> _listeners = new List<Action<NativeIOConnectionEvent>>();
        private sealed class ListenerToken : IDisposable
        {
            private MachineNativeIO _owner;
            private Action<NativeIOConnectionEvent> _callback;
            internal ListenerToken(MachineNativeIO owner, Action<NativeIOConnectionEvent> callback)
            { _owner = owner; _callback = callback; }
            /// <summary>Performs the dispose operation.</summary>
            public void Dispose()
            {
                MachineThread.Verify();
                if (_owner != null) _owner._listeners.Remove(_callback);
                _owner = null; _callback = null;
            }
        }

        /// <summary>Observe committed native pipe topology changes; unsubscribe on consumer unload.</summary>
        public IDisposable ObserveConnections(Action<NativeIOConnectionEvent> callback)
        {
            MachineThread.Verify();
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            _listeners.Add(callback);
            return new ListenerToken(this, callback);
        }

        internal void Publish(FlowPipe pipe, int flowIndex, bool connected)
        {
            if (pipe == null || _listeners.Count == 0) return;
            var measurement = GetMetrics(pipe);
            var evt = new NativeIOConnectionEvent(new NativeIOPipeSnapshot(pipe, flowIndex,
                measurement.TotalTransfers, measurement.AverageTransfersPerSecond), connected);
            foreach (var cb in _listeners.ToArray())
                try { cb(evt); } catch (Exception error) { MachineRuntime.ReportCallbackError("Native IO connection observer", error); }
        }
        internal void Shutdown() => _listeners.Clear();

        private static int FlowOf(FlowElement machine)
        {
            var player = MachineThread.Player;
            if (machine == null || player == null) return -1;
            for (int i = 0; i < player.Flows.Count; i++)
                if (player.Flows[i].Elements.Contains(machine)) return i;
            return -1;
        }
        private static int FlowOf(FlowPipe pipe)
        {
            var player = MachineThread.Player;
            if (pipe == null || player == null) return -1;
            for (int i = 0; i < player.Flows.Count; i++)
                if (player.Flows[i].Pipes.Contains(pipe)) return i;
            return -1;
        }
        private static KingdomFoundation.Api.FlowPipeMetrics GetMetrics(FlowPipe pipe)
            => KingdomFoundation.Api.FoundationApi.IO.GetMeasuredThroughput(pipe);

        /// <summary>Performs the ports operation.</summary>
        public IReadOnlyList<NativeIOPortSnapshot> Ports(FlowElement machine)
        {
            MachineThread.Verify();
            int flow = FlowOf(machine);
            if (flow < 0) throw new InvalidOperationException("Machine is not part of the loaded native kingdom.");
            var result = new List<NativeIOPortSnapshot>(machine.Inputs.Count + machine.Outputs.Count);
            foreach (var p in machine.Inputs) result.Add(new NativeIOPortSnapshot(p, flow));
            foreach (var p in machine.Outputs) result.Add(new NativeIOPortSnapshot(p, flow));
            return result.AsReadOnly();
        }
        /// <summary>Performs the port operation.</summary>
        public NativeIOPortSnapshot Port(FlowIO port)
        {
            MachineThread.Verify();
            if (port == null) throw new ArgumentNullException(nameof(port));
            int flow = FlowOf(port.Parent);
            if (flow < 0 || !(port is FlowInput ? port.Parent.Inputs.Contains((FlowInput)port)
                : port is FlowOutput && port.Parent.Outputs.Contains((FlowOutput)port)))
                throw new InvalidOperationException("Port is not an active native machine port.");
            return new NativeIOPortSnapshot(port, flow);
        }
        /// <summary>Performs the pipe operation.</summary>
        public NativeIOPipeSnapshot Pipe(FlowPipe pipe)
        {
            MachineThread.Verify();
            int flow = FlowOf(pipe);
            if (flow < 0) throw new InvalidOperationException("Pipe is not in an active native flow.");
            var metrics = GetMetrics(pipe);
            return new NativeIOPipeSnapshot(pipe, flow, metrics.TotalTransfers, metrics.AverageTransfersPerSecond);
        }
        /// <summary>Performs the pipes operation.</summary>
        public IReadOnlyList<NativeIOPipeSnapshot> Pipes(int flowIndex)
        {
            MachineThread.Verify();
            var player = MachineThread.Player;
            if (player == null || flowIndex < 0 || flowIndex >= player.Flows.Count)
                return new List<NativeIOPipeSnapshot>().AsReadOnly();
            var list = new List<NativeIOPipeSnapshot>();
            foreach (var pipe in player.Flows[flowIndex].Pipes)
            {
                if (pipe == null) continue;
                var measurement = GetMetrics(pipe);
                list.Add(new NativeIOPipeSnapshot(pipe, flowIndex,
                    measurement.TotalTransfers, measurement.AverageTransfersPerSecond));
            }
            return list.AsReadOnly();
        }
        /// <summary>Real observed pipe events entering/leaving a machine, without a simulated rated throughput.</summary>
        public NativeIOMachineThroughputSnapshot Throughput(FlowElement machine)
        {
            MachineThread.Verify();
            int index = FlowOf(machine);
            if (index < 0) throw new InvalidOperationException("Machine is not part of a loaded kingdom flow.");
            long input = 0, output = 0;
            double inRate = 0, outRate = 0;
            foreach (var pipe in MachineThread.Player.Flows[index].Pipes)
            {
                if (pipe == null) continue;
                bool inbound = ReferenceEquals(pipe.ConnectedInput?.Parent, machine);
                bool outbound = ReferenceEquals(pipe.ConnectedOutput?.Parent, machine);
                if (!inbound && !outbound) continue;
                var m = GetMetrics(pipe);
                if (inbound) { input += m.TotalTransfers; inRate += m.AverageTransfersPerSecond; }
                if (outbound) { output += m.TotalTransfers; outRate += m.AverageTransfersPerSecond; }
            }
            var nativeProduction = FoundationMachineApi.Production.NativeOutputRate(machine);
            return new NativeIOMachineThroughputSnapshot(machine.Guid, index, input, output, inRate, outRate, nativeProduction);
        }

        /// <summary>Performs the validate connection operation.</summary>
        public NativeIOValidation ValidateConnection(FlowOutput output, FlowInput input)
        {
            MachineThread.Verify();
            if (output == null || input == null) return NativeIOValidation.No("Both native port endpoints are required.");
            if (output.Parent == null || input.Parent == null) return NativeIOValidation.No("Both ports require native parents.");
            int from = FlowOf(output.Parent), to = FlowOf(input.Parent);
            if (from < 0 || to < 0) return NativeIOValidation.No("Both machines must exist in the loaded kingdom.");
            if (from != to) return NativeIOValidation.No("The native editor requires a single flow for both endpoints.");
            if (ReferenceEquals(output.Parent, input.Parent)) return NativeIOValidation.No("The existing Foundation connection helper does not support self-connections.");
            if (!output.Parent.Outputs.Contains(output) || !input.Parent.Inputs.Contains(input))
                return NativeIOValidation.No("At least one port has been removed from its machine.");
            if (output.ConnectedPipe != null || input.ConnectedPipe != null)
                return NativeIOValidation.No("A port already has a pipe; native replacement requires disconnection.");
            var manager = WorldFlowManager.Instance;
            if (!manager || manager.CurrentFlow != MachineThread.Player.Flows[from])
                return NativeIOValidation.No("Native editing requires this flow to be visible.");
            if (!output.World || !input.World)
                return NativeIOValidation.No("Native IO world views are unavailable.");
            // The game's WorldFlowIO._makeNewConnection checks direction and
            // occupancy only. No item-type compatibility check occurs when
            // creating a pipe; rejection happens later in FlowPipe.Update.
            return NativeIOValidation.Yes();
        }
        /// <summary>Ask the real native input predicate about a concrete item, without enqueuing it.</summary>
        public NativeIOValidation CanAccept(FlowInput input, InventoryItem item)
        {
            MachineThread.Verify();
            if (input == null || item == null) return NativeIOValidation.No("A real native input and item are required.");
            if (FlowOf(input.Parent) < 0 || !input.Parent.Inputs.Contains(input))
                return NativeIOValidation.No("Input is not present in the current kingdom.");
            if (!input.CanAcceptItem(item)) return NativeIOValidation.No("Native FlowIO.CanAcceptItem rejects this item type/contents.");
            if (!input.CanBuffer) return NativeIOValidation.No("Native input buffer is full.");
            return NativeIOValidation.Yes();
        }
        /// <summary>Performs the connect operation.</summary>
        public MachineOperationResult Connect(FlowOutput output, FlowInput input, IEnumerable<Vector2Int> route = null)
        {
            var check = ValidateConnection(output, input);
            if (!check.Allowed) return MachineOperationResult.Reject(check.Reason);
            try
            {
                // Delegate to the EXISTING Foundation native workspace bridge;
                // no competing connection or pipe graph implementation.
                KingdomFoundation.Api.FoundationApi.IO.Connect(output, input, route);
                return MachineOperationResult.Ok("Native WorldFlowManager.CreateNewPipe completed.");
            }
            catch (Exception error) { MachineRuntime.ReportCallbackError("Native pipe connect", error); return MachineOperationResult.Reject(error.Message); }
        }
        /// <summary>Performs the disconnect operation.</summary>
        public MachineOperationResult Disconnect(FlowPipe pipe)
        {
            MachineThread.Verify();
            if (FlowOf(pipe) < 0) return MachineOperationResult.Reject("Pipe is not in the active kingdom.");
            try { KingdomFoundation.Api.FoundationApi.IO.Disconnect(pipe); return MachineOperationResult.Ok("Native pipe disconnected."); }
            catch (Exception error) { MachineRuntime.ReportCallbackError("Native pipe disconnect", error); return MachineOperationResult.Reject(error.Message); }
        }
        public MachineOperationResult ReplaceRoute(FlowPipe pipe, IEnumerable<Vector2Int> points)
        {
            MachineThread.Verify();
            if (FlowOf(pipe) < 0 || points == null) return MachineOperationResult.Reject("An active pipe and non-null route are required.");
            try { KingdomFoundation.Api.FoundationApi.IO.ReplaceRoute(pipe, points); return MachineOperationResult.Ok("Native route and line/collider updated."); }
            catch (Exception error) { MachineRuntime.ReportCallbackError("Native pipe route", error); return MachineOperationResult.Reject(error.Message); }
        }
        /// <summary>Native grid snap is opt-in, matching Ctrl+drag unsnapped editor behavior.</summary>
        public Vector2Int SnapRouteNode(Vector2Int position)
        {
            MachineThread.Verify();
            return WorldFlowPipeNode.ClampToGrid(position);
        }
        public MachineOperationResult InsertRouteNode(FlowPipe pipe, int index, Vector2Int position)
        {
            MachineThread.Verify();
            if (FlowOf(pipe) < 0) return MachineOperationResult.Reject("Pipe is not in an active native flow.");
            try { KingdomFoundation.Api.FoundationApi.IO.InsertRouteNode(pipe, index, position); return MachineOperationResult.Ok(); }
            catch (Exception error) { MachineRuntime.ReportCallbackError("Insert native route node", error); return MachineOperationResult.Reject(error.Message); }
        }
        /// <summary>Removes route node.</summary>
        public MachineOperationResult RemoveRouteNode(FlowPipe pipe, int index)
        {
            MachineThread.Verify();
            if (FlowOf(pipe) < 0) return MachineOperationResult.Reject("Pipe is not in an active native flow.");
            try { KingdomFoundation.Api.FoundationApi.IO.RemoveRouteNode(pipe, index); return MachineOperationResult.Ok(); }
            catch (Exception error) { MachineRuntime.ReportCallbackError("Remove native route node", error); return MachineOperationResult.Reject(error.Message); }
        }
        public NativeIORouterSnapshot Router(FlowElement machine)
        {
            MachineThread.Verify();
            if (FlowOf(machine) < 0) throw new InvalidOperationException("Router is not part of the active kingdom.");
            if (machine is Collector c) return new NativeIORouterSnapshot(c, c.CurrentInput, null, c.LastItem?.Type?.Identifier, 4);
            if (machine is Distributor d) return new NativeIORouterSnapshot(d, null, d.CurrentOutput, d.LastItem?.Type?.Identifier, 4);
            if (machine is Manifold m) return new NativeIORouterSnapshot(m, null, null, m.LastItem?.Type?.Identifier, 4);
            return new NativeIORouterSnapshot(machine, null, null, null, 0);
        }
        /// <summary>Delegates to the game's AddInputSlot/AddOutputSlot, never patches a private count.</summary>
        public MachineOperationResult ExpandRouter(FlowElement machine)
        {
            MachineThread.Verify();
            return FoundationMachineApi.Operations.ExpandPorts(machine);
        }
        /// <summary>Delegates to the game's Collapse; preserve buffered items in trailing slots.</summary>
        public MachineOperationResult CollapseRouter(FlowElement machine)
        {
            MachineThread.Verify();
            // Vanilla Collapse only checks pipe connections, not buffered items.
            // A pure convenience wrapper should not silently discard live items.
            if (FlowOf(machine) < 0) return MachineOperationResult.Reject("Router is not part of the kingdom.");
            if (machine is Collector c)
            {
                for (int i = c.Inputs.Count - 1; i >= 2; i--)
                {
                    if (c.Inputs[i].ConnectedPipe != null) { break; }
                    if (c.Inputs[i].HasBuffer) return MachineOperationResult.Reject("Trailing collector input has buffered items; native Collapse would remove it.");
                }
            }
            else if (machine is Distributor d)
            {
                for (int i = d.Outputs.Count - 1; i >= 2; i--)
                {
                    if (d.Outputs[i].ConnectedPipe != null) { break; }
                    if (d.Outputs[i].HasBuffer) return MachineOperationResult.Reject("Trailing distributor output has buffered items; native Collapse would remove it.");
                }
            }
            else if (machine is Manifold m)
            {
                for (int i = m.Inputs.Count - 1; i >= 2; i--)
                {
                    if (m.Inputs[i].ConnectedPipe != null || m.Outputs[i].ConnectedPipe != null) { break; }
                    if (m.Inputs[i].HasBuffer || m.Outputs[i].HasBuffer)
                        return MachineOperationResult.Reject("Trailing manifold ports contain buffered items; native Collapse would remove them.");
                }
            }
            return FoundationMachineApi.Operations.CollapsePorts(machine);
        }
    }
}
