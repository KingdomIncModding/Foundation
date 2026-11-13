using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class FoundationPanelTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal FoundationPanelHost Host;
        internal string Message;
        public void OnPointerEnter(PointerEventData data)
        {
            if (Host)
                Host.Tooltip(Message);
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (Host)
                Host.Tooltip(null);
        }
    }
}
