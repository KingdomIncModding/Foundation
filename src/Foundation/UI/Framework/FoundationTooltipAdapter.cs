using System;
using System.Linq;
using Behaviour.UI.Tooltip;
using UnityEngine;
using Behaviour.UI;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationTooltipAdapter : TooltipSource
    {
        internal GameObject Source;
        public override void AddCustomContent(UITooltip tooltip)
        {
            base.AddCustomContent(tooltip);
            try{if(Source)Source.GetComponentsInParent<MonoBehaviour>(true).OfType<IFoundationTooltipCustomSource>().FirstOrDefault()?.AddTooltipCustomContent(new FoundationTooltipContent(tooltip));}
            catch(Exception error){FoundationApi.Report(error);}
        }
        public override string GetTitle()
        {
            if(!string.IsNullOrEmpty(Title))return Title;
            try{return Source ? Source.GetComponentsInParent<MonoBehaviour>(true).OfType<IFoundationTooltipTitleSource>().FirstOrDefault()?.GetTooltipTitle() : null;}
            catch(Exception error){FoundationApi.Report(error);return null;}
        }
        public override string GetBodyText()
        {
            if(!string.IsNullOrEmpty(BodyText))return BodyText;
            try{return Source ? Source.GetComponentsInParent<MonoBehaviour>(true).OfType<IFoundationTooltipTextSource>().FirstOrDefault()?.GetTooltipText() : null;}
            catch(Exception error){FoundationApi.Report(error);return null;}
        }
    }
}
