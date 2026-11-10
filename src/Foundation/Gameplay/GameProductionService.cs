using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameProductionService : IGameProductionService
    {
        static GamePlayer Player => GameWorldService.RequirePlayer();
        /// <summary>Gets or sets the forest value.</summary>
        public ForestSettings Forest => Player.ForestSettings;
        public FarmSettings Farm => Player.FarmSettings;
        public IReadOnlyList<ForestItemType> ForestResources => Array.AsReadOnly(Forest?.Resources.ToArray() ?? Array.Empty<ForestItemType>());
        public IReadOnlyList<FarmItemType> FarmResources => Array.AsReadOnly(Farm?.Resources.ToArray() ?? Array.Empty<FarmItemType>());
        static ForestSettings RequireForest() => GameWorldService.RequirePlayer().ForestSettings ??
            throw new InvalidOperationException("The native forest has not been unlocked.");
        static FarmSettings RequireFarm() => GameWorldService.RequirePlayer().FarmSettings ??
            throw new InvalidOperationException("The native farm has not been unlocked.");
        /// <summary>Performs the forest workers operation.</summary>
        public int ForestWorkers(ForestWorker type) => RequireForest().GetWorkerCount(type);
        public int TotalForestWorkers => RequireForest().TotalWorkers;
        public bool ForestBlocked => RequireForest().IsBlocked;
        public bool FarmBlocked => RequireFarm().IsBlocked;
        public float ForestUtilization(ForestWorker role) => RequireForest().GetUtilization(role);
        public float ForestQuotaMultiplier(ForestWorker role) => RequireForest().GetResourceQuotaMultiplier(role);
        public bool ForestResourceUnlocked(ForestItemType resource)
            => RequireForest().ItemUnlocked(resource ?? throw new ArgumentNullException(nameof(resource)));
        /// <summary>Performs the farm resource unlocked operation.</summary>
        public bool FarmResourceUnlocked(FarmItemType resource)
            => RequireFarm().ItemUnlocked(resource ?? throw new ArgumentNullException(nameof(resource)));
        public CurrencyCollection NextForestWorkerCost => RequireForest().NextWorkerCost;
        public CurrencyCollection FarmExpansionCost(int addedPlots)
        {
            if (addedPlots < 0) throw new ArgumentOutOfRangeException(nameof(addedPlots));
            return RequireFarm().GetPlotCost(addedPlots);
        }

        /// <summary>Attempts to hire forest worker.</summary>
        public bool TryHireForestWorker()
        {
            var forest = RequireForest();
            var cost = forest.NextWorkerCost;
            if (!Player.SpendCurrency(cost)) return false;
            try
            {
                if (forest.AddWorkerCount(ForestWorker.Unassigned, 1)) return true;
                Player.AddCurrency(cost, false);
                return false;
            }
            catch
            {
                Player.AddCurrency(cost, false);
                throw;
            }
        }
        /// <summary>Performs the assign forest workers operation.</summary>
        public bool AssignForestWorkers(ForestWorker role, int number)
        {
            if (role == ForestWorker.Unassigned) throw new ArgumentException("Select a worker role.", nameof(role));
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number));
            return RequireForest().AddWorkerCount(role, number);
        }
        public bool UnassignForestWorkers(ForestWorker role, int number)
        {
            if (role == ForestWorker.Unassigned) throw new ArgumentException("Select a worker role.", nameof(role));
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number));
            return RequireForest().AddWorkerCount(role, -number);
        }
        /// <summary>Performs the unlock forest resource operation.</summary>
        public void UnlockForestResource(ForestItemType type)
            => RequireForest().UnlockItem(type ?? throw new ArgumentNullException(nameof(type)));
        public void UnlockFarmResource(FarmItemType type)
            => RequireFarm().UnlockItem(type ?? throw new ArgumentNullException(nameof(type)));
        public FarmItemType GetCrop(int x, int y)
        {
            var farm = RequireFarm();
            ValidateCell(farm, x, y);
            return farm.GetItem(x, y);
        }
        static void ValidateCell(FarmSettings farm, int x, int y)
        {
            if (x < 0 || x >= farm.Width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= farm.Height) throw new ArgumentOutOfRangeException(nameof(y));
        }
        /// <summary>Performs the plant crop operation.</summary>
        public void PlantCrop(int x, int y, FarmItemType type)
        {
            var farm = RequireFarm();
            ValidateCell(farm, x, y);
            if (type != null && !farm.ItemUnlocked(type)) throw new InvalidOperationException("Crop is not unlocked.");
            farm.SetItem(x, y, type);
            farm.UpdateResourceQuota();
        }
        /// <summary>Attempts to expand farm.</summary>
        public bool TryExpandFarm(int width, int height)
        {
            var farm = RequireFarm();
            if (width < farm.Width || height < farm.Height || width > FarmSettings.MaxWidth ||
                height > FarmSettings.MaxWidth || width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Only valid farm expansions are supported.");
            int cellsAdded = width * height - farm.Width * farm.Height;
            if (cellsAdded == 0) return true;
            var cost = farm.GetPlotCost(cellsAdded);
            if (!Player.SpendCurrency(cost)) return false;
            try { farm.SetFieldSize(width, height); farm.UpdateResourceQuota(); return true; }
            catch { Player.AddCurrency(cost, false); throw; }
        }
    }
}
