using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Read-only, snapshot-based access to the game's actual FlowElement/FlowIO/FlowPipe
    /// implementation. This API intentionally does not invent throughput limits or
    /// expose mutable inventory buffers as framework-owned collections.
    /// Call from Unity's main thread. Snapshots describe the instant they were taken.
    /// </summary>
    public interface IFlowInspectionService
    {
        FlowElementSnapshot Inspect(FlowElement element);
        FlowPortSnapshot Inspect(FlowIO port);
        FlowPipeSnapshot Inspect(FlowPipe pipe);
        IReadOnlyList<FlowElementSnapshot> InspectElements(FlowDiagram diagram);
        IReadOnlyList<FlowPipeSnapshot> InspectPipes(FlowDiagram diagram);

        /// <summary>Raised after native FlowPipe.Connect(output, input, ghost:false).</summary>
        event Action<FlowPipeSnapshot> PipeConnected;
        /// <summary>Raised after a native FlowDiagram.DisconnectPipe call removes a pipe.</summary>
        event Action<FlowPipeSnapshot> PipeDisconnected;
        /// <summary>
        /// Raised after native WorldFlowPipe.ItemTransferred reports a successful
        /// transfer in the live world (one event per transferred item).
        /// Detached/no-world pipe simulations cannot raise this event.
        /// </summary>
        event Action<FlowItemTransferSnapshot> ItemTransferred;
    }
}
