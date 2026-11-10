using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameEconomyService : IGameEconomyService
    {
        static GamePlayer Player => GameWorldService.RequirePlayer();
        /// <summary>Performs the wallet operation.</summary>
        public IReadOnlyDictionary<Currency, long> Wallet()
        {
            var result = new Dictionary<Currency, long>();
            foreach (Currency c in Enum.GetValues(typeof(Currency))) result.Add(c, Player.GetCurrency(c));
            return result;
        }
        public long Get(Currency currency) => Player.GetCurrency(currency);
        public bool CanAfford(CurrencyCollection price) => Player.CanAfford(price ?? throw new ArgumentNullException(nameof(price)));
        /// <summary>Attempts to spend.</summary>
        public bool TrySpend(CurrencyCollection price)
        {
            if (price == null) throw new ArgumentNullException(nameof(price));
            foreach (var entry in price.GetCurrency()) if (entry.amt < 0) throw new ArgumentOutOfRangeException(nameof(price));
            return Player.SpendCurrency(price);
        }
        public bool TrySpend(Currency currency, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return Player.SpendCurrency(currency, amount);
        }
        /// <summary>Performs the grant operation.</summary>
        public void Grant(Currency currency, long amount, bool countAsIncome = false)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Player.AddCurrency(currency, amount, countAsIncome);
        }
        public void Grant(CurrencyCollection amounts, bool countAsIncome = false)
        {
            if (amounts == null) throw new ArgumentNullException(nameof(amounts));
            foreach (var entry in amounts.GetCurrency()) if (entry.amt < 0) throw new ArgumentOutOfRangeException(nameof(amounts));
            Player.AddCurrency(amounts, countAsIncome);
        }
        /// <summary>Performs the exchange tax operation.</summary>
        public float ExchangeTax(Currency currency) => Player.GetExchangeTax(currency);
        public bool CanAfford(Currency currency, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return Player.CanAfford(currency, amount);
        }
        public double ResourceCostMultiplier(ResourceItemType resource)
            => Player.GetResourceCostMultiplier(resource ?? throw new ArgumentNullException(nameof(resource)));
        /// <summary>Performs the additional workspace cost operation.</summary>
        public CurrencyCollection AdditionalWorkspaceCost() => Player.GetExtraWorkflowCost();

        public double ResourceAvailable(ResourceItemType resource) => Player.GetResourceAvailable(resource ?? throw new ArgumentNullException(nameof(resource)));
        public double ResourceDemand(ResourceItemType resource) => Player.GetResourceDemand(resource ?? throw new ArgumentNullException(nameof(resource)));
        public float ResourceQuotaMultiplier(ResourceItemType resource) => Player.GetResourceQuotaMultiplier(resource ?? throw new ArgumentNullException(nameof(resource)));
        public float ResourceOutput(ResourceElement element) => Player.GetResourceOutput(element ?? throw new ArgumentNullException(nameof(element)));
        public IReadOnlyList<ResourceAvailability> Resources()
        {
            var result = new List<ResourceAvailability>();
            var player = Player;
            foreach (var entry in player.GetAvailableResources())
                result.Add(new ResourceAvailability(entry.resource, entry.quota,
                    player.GetResourceDemand(entry.resource), player.GetResourceQuotaMultiplier(entry.resource)));
            return result.AsReadOnly();
        }
        /// <summary>Performs the resource upgrade operation.</summary>
        public ResourceUpgradeOffer ResourceUpgrade(ResourceItemType resource)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            var offer = Player.GetResourceUpgrade(resource);
            return new ResourceUpgradeOffer(offer.quota, offer.cost);
        }
        public void GrantResource(ResourceItemType resource, double amount, bool countsAsUpgrade = false)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Player.AddResource(resource, amount, countsAsUpgrade);
        }
        /// <summary>Sets farm quota.</summary>
        public void SetFarmQuota(FarmItemType resource, double amount)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Player.SetFarmResource(resource, amount);
        }
    }
}
