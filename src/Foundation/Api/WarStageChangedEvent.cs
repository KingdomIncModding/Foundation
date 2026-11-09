using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class WarStageChangedEvent
    {
        public WarProgress Progress { get; }
        public WarType War => Progress.War;
        public int From { get; }
        public int To { get; }
        public WarStageChangedEvent(WarProgress progress, int before, int after)
        { Progress = progress; From = before; To = after; }
    }
}
