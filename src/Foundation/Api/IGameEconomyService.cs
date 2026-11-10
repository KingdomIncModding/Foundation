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
        /// <summary>Returns the native player's currency balances as a read-only mapping.</summary>
        /// <returns>The requested native or Foundation result.</returns>
        IReadOnlyDictionary<Currency, long> Wallet();
        /// <summary>Returns the player's balance of the specified currency.</summary>
        /// <param name="currency">The currency value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        long Get(Currency currency);
        /// <summary>Checks whether the player's native wallet can cover the requested price or amount.</summary>
        /// <param name="price">The price value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool CanAfford(CurrencyCollection price);
        /// <summary>Attempts to deduct the specified price or amount using native economy rules; returns whether the deduction succeeded.</summary>
        /// <param name="price">The price value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool TrySpend(CurrencyCollection price);
        /// <summary>Attempts to deduct the specified price or amount using native economy rules; returns whether the deduction succeeded.</summary>
        /// <param name="currency">The currency value.</param>
        /// <param name="amount">The amount value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool TrySpend(Currency currency, long amount);
        /// <summary>Adds the specified currency or currencies to the player's wallet; optional income accounting is forwarded to the game.</summary>
        /// <param name="currency">The currency value.</param>
        /// <param name="amount">The amount value.</param>
        /// <param name="countAsIncome">The count as income value.</param>
        void Grant(Currency currency, long amount, bool countAsIncome = false);
        /// <summary>Adds the specified currency or currencies to the player's wallet; optional income accounting is forwarded to the game.</summary>
        /// <param name="amounts">The amounts value.</param>
        /// <param name="countAsIncome">The count as income value.</param>
        void Grant(CurrencyCollection amounts, bool countAsIncome = false);
        /// <summary>Returns the native exchange tax for the selected currency.</summary>
        /// <param name="currency">The currency value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        float ExchangeTax(Currency currency);
        /// <summary>Checks whether the player's native wallet can cover the requested price or amount.</summary>
        /// <param name="currency">The currency value.</param>
        /// <param name="amount">The amount value.</param>
        /// <returns>True when the operation succeeds or condition holds; otherwise false.</returns>
        bool CanAfford(Currency currency, long amount);
        /// <summary>Returns the native resource cost multiplier for the selected resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        double ResourceCostMultiplier(ResourceItemType resource);
        /// <summary>Returns the native additional-workspace cost.</summary>
        /// <returns>The requested native or Foundation result.</returns>
        CurrencyCollection AdditionalWorkspaceCost();

        /// <summary>Returns the native available quantity of the resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        double ResourceAvailable(ResourceItemType resource);
        /// <summary>Returns the native demand for the resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        double ResourceDemand(ResourceItemType resource);
        /// <summary>Returns the native quota multiplier for the resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        float ResourceQuotaMultiplier(ResourceItemType resource);
        /// <summary>Returns the native output for the specified producer.</summary>
        /// <param name="producer">The producer value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        float ResourceOutput(ResourceElement producer);
        /// <summary>Returns native resource availability entries.</summary>
        /// <returns>The requested native or Foundation result.</returns>
        IReadOnlyList<ResourceAvailability> Resources();
        /// <summary>Returns the native upgrade offer for the resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <returns>The requested native or Foundation result.</returns>
        ResourceUpgradeOffer ResourceUpgrade(ResourceItemType resource);
        /// <summary>Grants a resource quantity through the game's economy API.</summary>
        /// <param name="resource">The resource value.</param>
        /// <param name="amount">The amount value.</param>
        /// <param name="countsAsUpgrade">The counts as upgrade value.</param>
        void GrantResource(ResourceItemType resource, double amount, bool countsAsUpgrade = false);
        /// <summary>Sets the native quota for the farm resource.</summary>
        /// <param name="resource">The resource value.</param>
        /// <param name="amount">The amount value.</param>
        void SetFarmQuota(FarmItemType resource, double amount);
    }
}
