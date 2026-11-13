using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Mod-owned drag behavior. Foundation adapts it to the native drag manager.</summary>
    public interface IGameDragDropTarget
    {
        void OnDragDropMove(Vector2Int mousePosition, Vector2Int grabOffset);
        Vector2Int GetGrabOffset(Vector2Int mousePosition);
        void OnDragDropEnd();
        void OnDragDropCancel();
    }
}
