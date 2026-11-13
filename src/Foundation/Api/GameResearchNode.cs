using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Native research access through a stable Foundation identity and value types.</summary>
    public sealed class GameResearchNode
    {
        private static readonly ConditionalWeakTable<TechNode,GameResearchNode> Cache=new ConditionalWeakTable<TechNode,GameResearchNode>();
        internal TechNode Native { get; }
        private GameResearchNode(TechNode native) { Native=native; }
        internal static GameResearchNode Wrap(TechNode native)=>native==null ? null : Cache.GetValue(native,value=>new GameResearchNode(value));
        public string Identifier=>Native.Identifier;
        public string Name=>Native.Name;
        public string Description=>Native.Description;
        public GameResearchNode Root=>Wrap(Native.Root);
        public IReadOnlyList<GameResearchNode> Requirements=>Native.AllRequirements.Select(Wrap).ToArray();
        public bool HasCustomRequirement=>Native.CustomRequirement!=null;
        public string CustomRequirementDescription=>Native.CustomRequirementDesc;
        public bool CheckCustomRequirement(GameKingdom kingdom)=>Native.CustomRequirement==null || Native.CustomRequirement(kingdom.Native);
        public int CostTier=>Native.CostTier;
        public IReadOnlyList<string> CostCurrencies=>Array.AsReadOnly(Native.CostCurrency.Select(value=>value.ToString()).ToArray());
        public void SetCostCurrencies(IEnumerable<string> currencies)
        { if(currencies==null)throw new ArgumentNullException(nameof(currencies));Native.CostCurrency=currencies.Select(GameCurrencyMap.Resolve).ToArray(); }
        public GameMoney GetCost()=>new GameMoney(Native.GetCost());
        public void ResetCost()=>Native.ResetCost();
    }
}
