using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameProgressService : IGameProgressService
    {
        static GamePlayer Player => GameWorldService.RequirePlayer();
        public IReadOnlyList<TechNode> Research => Array.AsReadOnly(TechNode.Nodes.ToArray());
        public TechNode FindTech(string id) => id == null ? null : TechNode.Get(id);
        public bool HasTech(string id) => FindTech(id) is TechNode node && Player.HasTech(node);
        public IReadOnlyList<TechNode> Requirements(TechNode node)
            => Array.AsReadOnly((node ?? throw new ArgumentNullException(nameof(node))).AllRequirements.ToArray());
        public IReadOnlyList<TechNode> AvailableChildren(TechNode node)
            => Array.AsReadOnly((node ?? throw new ArgumentNullException(nameof(node))).AvailableTech.ToArray());
        public bool ResearchAvailable(TechNode node)
            => (node ?? throw new ArgumentNullException(nameof(node))).IsAvailable;
        public bool ResearchLocked(TechNode node)
            => (node ?? throw new ArgumentNullException(nameof(node))).IsLocked;
        public CurrencyCollection ResearchCost(TechNode node)
            => (node ?? throw new ArgumentNullException(nameof(node))).GetCost();
        public int ResearchCostTier(TechNode node)
            => (node ?? throw new ArgumentNullException(nameof(node))).CostTier;

        public bool UnlockTech(string id, bool notify = true)
        {
            var node = FindTech(id);
            if (node == null) throw new ArgumentException("Unknown research identifier: " + id, nameof(id));
            if (node.DevMode || Player.HasTech(node)) return false;
            NativeResearch.EnsureCapacity(Player);
            Player.AddTech(node, notify);
            return true;
        }
        public int UpgradeSlots(string elementType) => Player.GetUpgradeSlotCount(elementType ?? throw new ArgumentNullException(nameof(elementType)));
        public void SetUpgradeSlots(string elementType, int slots)
        {
            if (string.IsNullOrWhiteSpace(elementType)) throw new ArgumentException("Element type required.", nameof(elementType));
            if (slots < 0) throw new ArgumentOutOfRangeException(nameof(slots));
            Player.SetUpgradeSlotCount(elementType, slots);
        }
        public void UnlockUpgrade(string elementType, string upgradeType)
        {
            if (string.IsNullOrWhiteSpace(elementType)) throw new ArgumentException("Element type required.", nameof(elementType));
            if (string.IsNullOrWhiteSpace(upgradeType)) throw new ArgumentException("Upgrade type required.", nameof(upgradeType));
            Player.AddAvailableFlowUpgrade(elementType, upgradeType);
        }
        public IReadOnlyList<string> UnlockedUpgrades(string elementType)
        {
            if (elementType == null) throw new ArgumentNullException(nameof(elementType));
            var upgrades = HarmonyLib.AccessTools.Field(typeof(GamePlayer), "_unlockedFlowUpgrades");
            var available = (Dictionary<string, HashSet<string>>)upgrades.GetValue(Player);
            return available.TryGetValue(elementType, out var set)
                ? Array.AsReadOnly(set.OrderBy(s => s, StringComparer.Ordinal).ToArray())
                : Array.Empty<string>();
        }
        public IReadOnlyList<PrestigeNode> Prestige => Array.AsReadOnly(PrestigeNode.All.ToArray());
        public int PrestigePoints(PrestigeNode node) => Player.GetPrestigePoints(node ?? throw new ArgumentNullException(nameof(node)));
        public void SetPrestigePoints(PrestigeNode node, int points)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
            Player.SetPrestigePoints(node, points);
        }
        public int Ascensions => Player.AscensionCount;
        public int AvailableNightmarePoints => Player.NightmareAvailable;
        public bool NightmareEnabled { get => Player.NightmareActive; set => Player.NightmareActive = value; }
    }
}
