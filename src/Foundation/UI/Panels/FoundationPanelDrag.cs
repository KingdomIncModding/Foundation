using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Flow;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class FoundationPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal FoundationPanelHost Host;
        public void OnBeginDrag(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            if (Host)
                Host.MoveBy(data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (Host)
                Host.SavePosition();
        }
    }
}
