using UnityEngine;

namespace KingdomFoundation.Api
{
    public sealed class FoundationTooltipSource : MonoBehaviour
    {
        private FoundationTooltipAdapter adapter;
        private FoundationTooltipAdapter Adapter
        {
            get
            {
                if(!adapter){adapter=gameObject.AddComponent<FoundationTooltipAdapter>();adapter.Source=gameObject;adapter.enabled=isActiveAndEnabled;}
                return adapter;
            }
        }
        public void SetSource(GameObject source){Adapter.Source=source;Adapter.SetSource(source);}
        public void SetTooltip(string title,string body)=>Adapter.SetTooltip(title,body);
        private void OnEnable(){if(adapter)adapter.enabled=true;}
        private void OnDisable(){if(adapter)adapter.enabled=false;}
        private void OnDestroy(){if(adapter)Destroy(adapter);}
    }
}
