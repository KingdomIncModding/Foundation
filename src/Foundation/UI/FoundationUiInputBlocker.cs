using Assets.Behaviour.UI.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    /// <summary>Native zoom blocking for mod-owned UI without inheriting a native game component.</summary>
    public sealed class FoundationUiInputBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private UIBlocksZoom native;
        private void Awake() { native = gameObject.AddComponent<UIBlocksZoom>(); }
        public void OnPointerEnter(PointerEventData eventData) { if (native) native.OnPointerEnter(eventData); }
        public void OnPointerExit(PointerEventData eventData) { if (native) native.OnPointerExit(eventData); }
        private void OnDisable() { OnPointerExit(null); }
        private void OnDestroy() { if (native) Destroy(native); }
    }
}
