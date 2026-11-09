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
        IReadOnlyList<WarProgress> ActiveWars { get; }
        Quest FindQuest(string id);
        DungeonType FindDungeon(string id);
        WarType FindWar(string id);
        int QuestCompletionCount(Quest quest);
        int QuestDuration(Quest quest);
        IReadOnlyList<QuestReward> QuestRewards(Quest quest);
        bool QuestUnlocked(Quest quest);
        bool IsDungeonAvailable(DungeonType dungeon);
        CurrencyCollection DungeonSupplyCost(DungeonType dungeon);
        IReadOnlyList<DungeonStage> DungeonStages(DungeonType dungeon);
        bool HasWarStage(WarType war, int stage);
        int WarCurrentStage(WarType war);

        QuestMetadata QuestMetadata(Quest quest);
        DungeonMetadata DungeonMetadata(DungeonType dungeon);
        DungeonProgress DungeonProgress(DungeonType dungeon);
        bool IsDungeonUnlocked(DungeonType dungeon);
        void UnlockDungeon(DungeonType dungeon);
        WarProgress WarProgress(WarType war);
        void UnlockWar(WarType war);
        IReadOnlyList<Armory> Armories(int channel, Assets.Source.Items.ItemType item);
        IReadOnlyList<Portal> Portals(int channel, int channelIndex);
    }
}
