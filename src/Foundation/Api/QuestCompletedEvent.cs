using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the quest completed event API.</summary>
    public sealed class QuestCompletedEvent
    {
        public QuestProgress Progress { get; }
        public Quest Quest { get; }
        public int TotalCompletions { get; }
        public QuestCompletedEvent(QuestProgress progress)
        { Progress = progress; Quest = progress.Quest; TotalCompletions = progress.CompletedTimes; }
    }
}
