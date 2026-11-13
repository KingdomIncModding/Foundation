using System;
using Assets.Behaviour.UI;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameDragDropAdapter : MonoBehaviour, IDragDropTarget
    {
        internal IGameDragDropTarget Target;
        private bool Alive => Target is MonoBehaviour owner && owner;
        private void Invoke(Action action)
        {
            if (!Alive) { ClearOwned(); return; }
            try { action(); } catch (Exception error) { ClearOwned(); FoundationApi.Report(error); }
        }
        public void OnDragDropMove(Vector2Int position, Vector2Int offset) => Invoke(() => Target.OnDragDropMove(position, offset));
        public Vector2Int GetGrabOffset(Vector2Int position)
        {
            if (!Alive) return Vector2Int.zero;
            try { return Target.GetGrabOffset(position); } catch (Exception error) { ClearOwned(); FoundationApi.Report(error); return Vector2Int.zero; }
        }
        public void OnDragDropEnd() => Invoke(() => Target.OnDragDropEnd());
        public void OnDragDropCancel() => Invoke(() => Target.OnDragDropCancel());
        private void ClearOwned() { if (DragDropManager.Instance && ReferenceEquals(DragDropManager.Instance.Target, this)) GameDragDropService.ClearNative(); }
        private void OnDisable() { ClearOwned(); }
        private void OnDestroy() { ClearOwned(); Target=null; }
    }
}
