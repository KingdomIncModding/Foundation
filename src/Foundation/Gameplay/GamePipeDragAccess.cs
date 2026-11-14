using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GamePipeDragAccess : IGamePipeDragAccess
    {
        private static readonly AccessTools.FieldRef<WorldFlowIO, WorldFlowPipe> Ghost =
            AccessTools.FieldRefAccess<WorldFlowIO, WorldFlowPipe>("_pipeGhost");

        public WorkspacePipeDrag Active => DragDropManager.Instance
            ? FromView(DragDropManager.Instance.Target as Component) : null;

        public WorkspacePipeDrag FromView(Component view)
        {
            var port = view as WorldFlowIO;
            var ghost = port ? Ghost(port) : null;
            return ghost && ghost.Pipe != null ? new WorkspacePipeDrag(port, ghost) : null;
        }

        public int OutwardDirection(WorkspacePort port)
            => port.IsOutput != port.Parent.Mirrored ? 1 : -1;

        public Vector2Int ConnectionPoint(WorkspacePort port)
        {
            if (port?.World == null) throw new System.ArgumentException("Expected a visible port.", nameof(port));
            return Vector2Int.RoundToInt((Vector2)port.World.transform.position + new Vector2(16 * OutwardDirection(port), 0));
        }
    }
}
