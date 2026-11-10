using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the igame progress service API.</summary>
    public interface IGameProgressService
    {
        IReadOnlyList<TechNode> Research { get; }
        TechNode FindTech(string id);
        bool HasTech(string id);
        IReadOnlyList<TechNode> Requirements(TechNode node);
        IReadOnlyList<TechNode> AvailableChildren(TechNode node);
        bool ResearchAvailable(TechNode node);
        /// <summary>Performs the research locked operation.</summary>
        bool ResearchLocked(TechNode node);
        CurrencyCollection ResearchCost(TechNode node);
        int ResearchCostTier(TechNode node);

        bool UnlockTech(string id, bool notify = true);
        int UpgradeSlots(string elementType);
        void SetUpgradeSlots(string elementType, int slots);
        void UnlockUpgrade(string elementType, string upgradeType);
        /// <summary>Performs the unlocked upgrades operation.</summary>
        IReadOnlyList<string> UnlockedUpgrades(string elementType);
        IReadOnlyList<PrestigeNode> Prestige { get; }
        int PrestigePoints(PrestigeNode node);
        void SetPrestigePoints(PrestigeNode node, int points);
        int Ascensions { get; }
        int AvailableNightmarePoints { get; }
        bool NightmareEnabled { get; set; }
    }
}
