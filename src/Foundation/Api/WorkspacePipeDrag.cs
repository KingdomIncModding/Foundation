using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Short-lived native pipe preview. Valid only while its view exists; main thread only.</summary>
    public sealed class WorkspacePipeDrag
    {
        private readonly WorldFlowIO owner;
        private readonly WorldFlowPipe ghost;
        internal WorkspacePipeDrag(WorldFlowIO owner, WorldFlowPipe ghost) { this.owner = owner; this.ghost = ghost; }
        public Component Owner => owner;
        public bool IsAlive => owner && ghost;
        public bool IsActive => IsAlive && DragDropManager.Instance && ReferenceEquals(DragDropManager.Instance.Target, owner);
        public WorkspacePipe Pipe => IsAlive ? WorkspacePipe.Wrap(ghost.Pipe) : null;
        public WorkspacePort Start => IsAlive ? WorkspacePort.Wrap(owner.IO) : null;
        public WorkspacePort HoveredPort
        {
            get
            {
                if (!IsAlive || owner.IO == null) return null;
                // Unity can retain a managed reference after destroying the native UI object.
                var hovered = UIHelper.GetMouseOverUI();
                if (!hovered) return null;
                var view = hovered.GetComponentInParent<WorldFlowIO>();
                return view && view != owner && view.IO != null && (view.IO is Assets.Source.Flow.FlowInput) != Start.IsInput
                    ? WorkspacePort.Wrap(view.IO) : null;
            }
        }
        public void SetPreview(IEnumerable<Vector2Int> nodes, Vector2Int endpoint)
        {
            if (!IsAlive) return;
            Pipe.SetNodes(nodes);
            ghost.GhostConnection = endpoint;
            ghost.UpdateLinePositions();
        }
        /// <summary>Return a rejected native drop to click-to-connect mode.</summary>
        public void Continue()
        {
            if (IsAlive && DragDropManager.Instance) DragDropManager.Instance.SetDragDropTarget(owner, false);
        }
    }
}
