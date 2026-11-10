using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Native forestry workforce allocation and crop-field management.</summary>
    public interface IGameProductionService
    {
        ForestSettings Forest { get; }
        FarmSettings Farm { get; }
        /// <summary>Exposes the forest resources value.</summary>
        IReadOnlyList<ForestItemType> ForestResources { get; }
        IReadOnlyList<FarmItemType> FarmResources { get; }
        int ForestWorkers(ForestWorker type);
        int TotalForestWorkers { get; }
        /// <summary>Gets or sets the forest blocked value.</summary>
        bool ForestBlocked { get; }
        bool FarmBlocked { get; }
        float ForestUtilization(ForestWorker role);
        float ForestQuotaMultiplier(ForestWorker role);
        /// <summary>Invokes the forest resource unlocked operation.</summary>
        /// <param name="resource">The resource argument.</param>
        /// <returns>The operation result.</returns>
        bool ForestResourceUnlocked(ForestItemType resource);
        bool FarmResourceUnlocked(FarmItemType resource);
        CurrencyCollection NextForestWorkerCost { get; }
        CurrencyCollection FarmExpansionCost(int addedPlots);

        /// <summary>Attempts to hire forest worker.</summary>
        bool TryHireForestWorker();
        bool AssignForestWorkers(ForestWorker role, int number);
        bool UnassignForestWorkers(ForestWorker role, int number);
        void UnlockForestResource(ForestItemType type);
        /// <summary>Invokes the unlock farm resource operation.</summary>
        /// <param name="type">The type argument.</param>
        void UnlockFarmResource(FarmItemType type);
        FarmItemType GetCrop(int x, int y);
        void PlantCrop(int x, int y, FarmItemType type);
        bool TryExpandFarm(int width, int height);
    }
}
