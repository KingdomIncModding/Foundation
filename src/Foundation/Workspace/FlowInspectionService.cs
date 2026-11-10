using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class FlowInspectionService : IFlowInspectionService
    {
        /// <summary>Occurs when pipe connected is raised.</summary>
        public event Action<FlowPipeSnapshot> PipeConnected;
        public event Action<FlowPipeSnapshot> PipeDisconnected;
        public event Action<FlowItemTransferSnapshot> ItemTransferred;

        public FlowElementSnapshot Inspect(FlowElement element) => new FlowElementSnapshot(element);
        public FlowPortSnapshot Inspect(FlowIO port) => new FlowPortSnapshot(port);
        public FlowPipeSnapshot Inspect(FlowPipe pipe) => new FlowPipeSnapshot(pipe);

        /// <summary>Performs the inspect elements operation.</summary>
        public IReadOnlyList<FlowElementSnapshot> InspectElements(FlowDiagram diagram)
        {
            if (diagram == null) throw new ArgumentNullException(nameof(diagram));
            var snapshots = new List<FlowElementSnapshot>(diagram.Elements.Count);
            foreach (var element in diagram.Elements)
                if (element != null) snapshots.Add(Inspect(element));
            return snapshots.AsReadOnly();
        }

        /// <summary>Performs the inspect pipes operation.</summary>
        public IReadOnlyList<FlowPipeSnapshot> InspectPipes(FlowDiagram diagram)
        {
            if (diagram == null) throw new ArgumentNullException(nameof(diagram));
            var snapshots = new List<FlowPipeSnapshot>(diagram.Pipes.Count);
            foreach (var pipe in diagram.Pipes)
                if (pipe != null) snapshots.Add(Inspect(pipe));
            return snapshots.AsReadOnly();
        }

        // Isolate subscriber exceptions: a third-party callback must not prevent
        // native Pipe.Connect/DisconnectPipe from completing.
        internal void RaiseConnected(FlowPipe pipe) => Raise(PipeConnected, new FlowPipeSnapshot(pipe));
        internal void RaiseDisconnected(FlowPipeSnapshot snapshot) => Raise(PipeDisconnected, snapshot);
        internal void RaiseItemTransferred(FlowPipe pipe, InventoryItem item)
            => RaiseTransfer(ItemTransferred, new FlowItemTransferSnapshot(pipe, item));

        private static void RaiseTransfer(Action<FlowItemTransferSnapshot> handlers, FlowItemTransferSnapshot snapshot)
        {
            if (handlers == null) return;
            foreach (Action<FlowItemTransferSnapshot> handler in handlers.GetInvocationList())
            {
                try { handler(snapshot); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
        }
        private static void Raise(Action<FlowPipeSnapshot> handlers, FlowPipeSnapshot snapshot)
        {
            if (handlers == null) return;
            foreach (Action<FlowPipeSnapshot> handler in handlers.GetInvocationList())
            {
                try { handler(snapshot); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
        }
    }
}
