using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameAdventureService : IGameAdventureService
    {
        static GamePlayer Player => GameWorldService.RequirePlayer();
        /// <summary>Gets or sets the quests value.</summary>
        public IReadOnlyList<Quest> Quests => Array.AsReadOnly(Quest.All.ToArray());
        public IReadOnlyList<DungeonType> Dungeons => Array.AsReadOnly(DungeonType.All.ToArray());
        public IReadOnlyList<WarProgress> ActiveWars => Array.AsReadOnly(Player.Wars.ToArray());
        public Quest FindQuest(string id) => Quest.All.FirstOrDefault(q => q.Identifier == id);
        public DungeonType FindDungeon(string id) => DungeonType.All.FirstOrDefault(d => d.Identifier == id);
        public WarType FindWar(string id)
        {
            if (id == null) return null;
            try { return WarType.Get(id); }
            catch (KeyNotFoundException) { return null; }
        }
        /// <summary>Performs the quest completion count operation.</summary>
        public int QuestCompletionCount(Quest quest) => QuestMetadata(quest).CompletedCount;
        public int QuestDuration(Quest quest) => (quest ?? throw new ArgumentNullException(nameof(quest))).TickDuration;
        public IReadOnlyList<QuestReward> QuestRewards(Quest quest)
        {
            if (quest == null) throw new ArgumentNullException(nameof(quest));
            return Array.AsReadOnly((quest.GetRewards(QuestMetadata(quest).CompletedCount) ?? Array.Empty<QuestReward>()).ToArray());
        }
        public bool QuestUnlocked(Quest quest) => (quest ?? throw new ArgumentNullException(nameof(quest))).IsUnlocked;
        /// <summary>Determines whether dungeon available.</summary>
        public bool IsDungeonAvailable(DungeonType dungeon)
            => (dungeon ?? throw new ArgumentNullException(nameof(dungeon))).IsAvailableFor(Player);
        public CurrencyCollection DungeonSupplyCost(DungeonType dungeon)
            => (dungeon ?? throw new ArgumentNullException(nameof(dungeon))).TotalSupplyCost;
        public IReadOnlyList<DungeonStage> DungeonStages(DungeonType dungeon)
            => Array.AsReadOnly((dungeon ?? throw new ArgumentNullException(nameof(dungeon))).Stages.ToArray());
        public bool HasWarStage(WarType war, int stage)
        {
            if (war == null) throw new ArgumentNullException(nameof(war));
            if (stage < 0) throw new ArgumentOutOfRangeException(nameof(stage));
            return Player.HasWarStage(war, stage);
        }
        /// <summary>Performs the war current stage operation.</summary>
        public int WarCurrentStage(WarType war)
            => Player.GetWarProgress(war ?? throw new ArgumentNullException(nameof(war)))?.CurrentStage ?? -1;

        public QuestMetadata QuestMetadata(Quest quest) => Player.GetQuestMetadata(quest ?? throw new ArgumentNullException(nameof(quest)));
        public DungeonMetadata DungeonMetadata(DungeonType dungeon) => Player.GetDungeonMetadata(dungeon ?? throw new ArgumentNullException(nameof(dungeon)));
        public DungeonProgress DungeonProgress(DungeonType dungeon) => Player.GetDungeonProgress(dungeon ?? throw new ArgumentNullException(nameof(dungeon)));
        public bool IsDungeonUnlocked(DungeonType dungeon) => Player.UnlockedDungeons.Contains((dungeon ?? throw new ArgumentNullException(nameof(dungeon))).Identifier);
        public void UnlockDungeon(DungeonType dungeon)
        {
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            Player.UnlockDungeon(dungeon);
        }
        /// <summary>Performs the war progress operation.</summary>
        public WarProgress WarProgress(WarType war) => Player.GetWarProgress(war ?? throw new ArgumentNullException(nameof(war)));
        public void UnlockWar(WarType war)
        {
            if (war == null) throw new ArgumentNullException(nameof(war));
            if (Player.GetWarProgress(war) == null) Player.UnlockWar(war);
        }
        public IReadOnlyList<Armory> Armories(int channel, Assets.Source.Items.ItemType item)
            => Array.AsReadOnly(Player.GetArmoryList(channel, item).ToArray());
        /// <summary>Performs the portals operation.</summary>
        public IReadOnlyList<Portal> Portals(int channel, int channelIndex)
            => Array.AsReadOnly(Player.GetPortalList(channel, channelIndex).ToArray());
    }
}
