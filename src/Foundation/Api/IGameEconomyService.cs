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
        /// <summary>Legacy single-precision adapter. Last native signature: game build 1.0.13.</summary>
        /// <remarks>Retained for compatibility; scheduled for removal in the next Foundation release.</remarks>
        [Obsolete("Deprecated: last working native signature was game build 1.0.13. Use ExchangeTaxDouble(Currency). Scheduled for removal in the next Foundation release.", false)]
        float ExchangeTax(Currency currency);
        /// <summary>Returns the native double-precision exchange tax, including the game's minimum tax.</summary>
        double ExchangeTaxDouble(Currency currency);
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
