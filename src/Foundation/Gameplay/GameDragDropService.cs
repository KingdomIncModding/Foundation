using System;
using Assets.Behaviour.UI;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameDragDropService : IGameDragDropService
    {
        private static readonly AccessTools.FieldRef<DragDropManager, IDragDropTarget> CurrentTarget = AccessTools.FieldRefAccess<DragDropManager, IDragDropTarget>("_currentTarget");
        public bool IsReady => DragDropManager.Instance;
        public IGameDragDropTarget Target => DragDropManager.Instance && DragDropManager.Instance.Target is GameDragDropAdapter adapter ? adapter.Target : null;
        public bool SetTarget(IGameDragDropTarget target, bool holdMode)
        {
            if (!(target is MonoBehaviour owner) || !owner) throw new ArgumentException("Drag targets must be live Unity behaviours.", nameof(target));
            if (!IsReady) return false;
            GameDragDropAdapter adapter = null;
            foreach (var candidate in owner.GetComponents<GameDragDropAdapter>())
                if (ReferenceEquals(candidate.Target, target)) { adapter=candidate; break; }
            if (!adapter) { adapter=owner.gameObject.AddComponent<GameDragDropAdapter>(); adapter.Target=target; }
            return DragDropManager.Instance.SetDragDropTarget(adapter, holdMode) && ReferenceEquals(DragDropManager.Instance.Target, adapter);
        }
        public void Clear() { ClearNative(); }
        internal static void ClearNative()
        {
            if (!DragDropManager.Instance) return;
            if (GameUI.Instance) DragDropManager.Instance.ClearDragDrop();
            else CurrentTarget(DragDropManager.Instance)=null;
        }
    }
}
