using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Native pipe-drag primitives. Route choice and editing policy belong to consumers.</summary>
    public interface IGamePipeDragAccess
    {
        WorkspacePipeDrag Active { get; }
        WorkspacePipeDrag FromView(Component view);
        Vector2Int ConnectionPoint(WorkspacePort port);
        int OutwardDirection(WorkspacePort port);
    }
}
