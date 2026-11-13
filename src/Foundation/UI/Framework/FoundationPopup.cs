using System;
using Assets.Behaviour.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    /// <summary>Mod-owned popup behaviour with a private Foundation adapter for native menu ownership.</summary>
    public class FoundationPopup : MonoBehaviour
    {
        private FoundationPopupAdapter adapter;
        public Action OnPopupClosed;
        public virtual bool CanCancel=>true;
        public virtual bool CanEscape=>true;
        private FoundationPopupAdapter Adapter
        {
            get
            {
                if(!adapter){adapter=gameObject.AddComponent<FoundationPopupAdapter>();adapter.Target=this;}
                return adapter;
            }
        }
        public void Show(bool visible,bool playSound=true)
        {if(!GameUI.Instance)throw new InvalidOperationException("Native GameUI is not active.");GameUI.Instance.TogglePopupMenu(Adapter,visible,playSound);}
        public void ClosePopup(){if(GameUI.Instance)Show(false);else gameObject.SetActive(false);}
        public virtual void PopupClosed()=>OnPopupClosed?.Invoke();
        public void OnPointerEnter(PointerEventData data)=>Adapter.OnPointerEnter(data);
        public void OnPointerExit(PointerEventData data){if(adapter)adapter.OnPointerExit(data);}
        protected virtual void OnDestroy(){if(adapter)Destroy(adapter);}
    }
}
