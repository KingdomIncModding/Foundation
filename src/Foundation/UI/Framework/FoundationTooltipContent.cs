using Behaviour.UI;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationTooltipContent : IFoundationTooltipContent
    {
        private readonly UITooltip native;
        internal FoundationTooltipContent(UITooltip native){this.native=native;}
        public void AddTextLine(string text)=>native.AddTextLine(text);
    }
}
