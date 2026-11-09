using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class MachineQuestChangedEvent
    {
        public QuestBoard Machine { get; }
        public Quest Previous { get; }
        public Quest Current { get; }
        public MachineQuestChangedEvent(QuestBoard machine, Quest previous, Quest current)
        { Machine = machine; Previous = previous; Current = current; }
    }
}
