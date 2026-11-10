using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomFoundation
{
    /// <summary>Drag a native-styled row onto another row, preserving the game UI event pipeline.</summary>
    internal sealed class FoundationPanelReorder : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IDropHandler
    {
        private static FoundationPanelReorder dragging;
        internal int Index;
        internal Action<int, int> Move;
        internal Transform Container;

        /// <summary>Performs the on begin drag operation.</summary>
        public void OnBeginDrag(PointerEventData data)
        {
            dragging = this;
        }
        public void OnDrag(PointerEventData data) { /* Native EventSystem supplies drop target. */ }
        public void OnDrop(PointerEventData data)
        {
            var source = dragging;
            if (source == null || source == this || source.Container != Container) return;
            dragging = null;
            try { source.Move?.Invoke(source.Index, Index); }
            catch (Exception error) { Api.FoundationApi.Report(error); }
        }
        /// <summary>Performs the on end drag operation.</summary>
        public void OnEndDrag(PointerEventData data)
        {
            if (dragging == this) dragging = null;
        }
        private void OnDisable()
        {
            if (dragging == this) dragging = null;
        }
    }
}
