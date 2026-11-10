using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the dungeon stage changed event API.</summary>
    public sealed class DungeonStageChangedEvent
    {
        public DungeonProgress Progress { get; }
        public DungeonType Dungeon => Progress.Dungeon;
        public int From { get; }
        public int To { get; }
        public DungeonStageChangedEvent(DungeonProgress progress, int before, int after)
        { Progress = progress; From = before; To = after; }
    }
}
