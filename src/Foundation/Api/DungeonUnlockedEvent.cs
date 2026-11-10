using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the dungeon unlocked event API.</summary>
    public sealed class DungeonUnlockedEvent
    {
        public GamePlayer Player { get; }
        public DungeonType Dungeon { get; }
        public DungeonUnlockedEvent(GamePlayer player, DungeonType dungeon) { Player = player; Dungeon = dungeon; }
    }
}
