using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Quests, wars, dungeon progression and the game's own adventurer stores.</summary>
    public interface IGameAdventureService
    {
        IReadOnlyList<Quest> Quests { get; }
        IReadOnlyList<DungeonType> Dungeons { get; }
        /// <summary>Exposes the active wars value.</summary>
        IReadOnlyList<WarProgress> ActiveWars { get; }
        Quest FindQuest(string id);
        DungeonType FindDungeon(string id);
        WarType FindWar(string id);
        /// <summary>Performs the quest completion count operation.</summary>
        int QuestCompletionCount(Quest quest);
        int QuestDuration(Quest quest);
        IReadOnlyList<QuestReward> QuestRewards(Quest quest);
        bool QuestUnlocked(Quest quest);
        /// <summary>Invokes the is dungeon available operation.</summary>
        /// <param name="dungeon">The dungeon argument.</param>
        /// <returns>The operation result.</returns>
        bool IsDungeonAvailable(DungeonType dungeon);
        CurrencyCollection DungeonSupplyCost(DungeonType dungeon);
        IReadOnlyList<DungeonStage> DungeonStages(DungeonType dungeon);
        bool HasWarStage(WarType war, int stage);
        /// <summary>Performs the war current stage operation.</summary>
        int WarCurrentStage(WarType war);

        QuestMetadata QuestMetadata(Quest quest);
        DungeonMetadata DungeonMetadata(DungeonType dungeon);
        /// <summary>Invokes the dungeon progress operation.</summary>
        /// <param name="dungeon">The dungeon argument.</param>
        /// <returns>The operation result.</returns>
        DungeonProgress DungeonProgress(DungeonType dungeon);
        bool IsDungeonUnlocked(DungeonType dungeon);
        void UnlockDungeon(DungeonType dungeon);
        WarProgress WarProgress(WarType war);
        /// <summary>Performs the unlock war operation.</summary>
        void UnlockWar(WarType war);
        IReadOnlyList<Armory> Armories(int channel, Assets.Source.Items.ItemType item);
        IReadOnlyList<Portal> Portals(int channel, int channelIndex);
    }
}
