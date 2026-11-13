using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Currency amounts independent of the native collection and enum types.</summary>
    public sealed class GameMoney
    {
        internal CurrencyCollection Native { get; }
        public GameMoney() { Native = new CurrencyCollection(); }
        public GameMoney(string currencyId, long amount)
        {
            var currency=GameCurrencyMap.Resolve(currencyId);
            Native = new CurrencyCollection(currency, amount);
        }
        internal GameMoney(CurrencyCollection amounts) { Native = new CurrencyCollection(); if (amounts != null) Native.Add(amounts); }
        public bool IsEmpty => Native.IsEmpty;
        public IReadOnlyList<GameMoneyEntry> Entries => Array.AsReadOnly(Native.GetCurrency()
            .Select(e => new GameMoneyEntry(e.c.ToString(), e.amt, e.c.GetIcon())).ToArray());
        public void Add(GameMoney amounts) { if (amounts != null) Native.Add(amounts.Native); }
        public void Add(string currencyId,long amount) { Native.Add(GameCurrencyMap.Resolve(currencyId),amount); }
    }
}
