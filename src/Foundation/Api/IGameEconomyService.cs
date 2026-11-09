using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Currency and resource APIs forwarding to native GamePlayer methods.</summary>
    public interface IGameEconomyService
    {
        IReadOnlyDictionary<Currency, long> Wallet();
        long Get(Currency currency);
        bool CanAfford(CurrencyCollection price);
        bool TrySpend(CurrencyCollection price);
        bool TrySpend(Currency currency, long amount);
        void Grant(Currency currency, long amount, bool countAsIncome = false);
        void Grant(CurrencyCollection amounts, bool countAsIncome = false);
        float ExchangeTax(Currency currency);
        bool CanAfford(Currency currency, long amount);
        double ResourceCostMultiplier(ResourceItemType resource);
        CurrencyCollection AdditionalWorkspaceCost();

        double ResourceAvailable(ResourceItemType resource);
        double ResourceDemand(ResourceItemType resource);
        float ResourceQuotaMultiplier(ResourceItemType resource);
        float ResourceOutput(ResourceElement producer);
        IReadOnlyList<ResourceAvailability> Resources();
        ResourceUpgradeOffer ResourceUpgrade(ResourceItemType resource);
        void GrantResource(ResourceItemType resource, double amount, bool countsAsUpgrade = false);
        void SetFarmQuota(FarmItemType resource, double amount);
    }
}
