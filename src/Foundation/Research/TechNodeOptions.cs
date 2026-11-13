using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Immutable native research rules. Register parent nodes before their children.</summary>
    public sealed class TechNodeOptions
    {
        /// <summary>Create research rules without binding the consumer to native enum or currency types.</summary>
        public static TechNodeOptions Create(GameMoney fixedCost = null, string nodeKind = "FlowElement", int costTier = 1,
            double costMultiplier = 1, double goldMultiplier = 1, string previousId = null, string rootId = null,
            IEnumerable<string> requirements = null, string iconName = "Icons_17")
        {
            if (!Enum.TryParse<TechNodeType>(nodeKind, out var kind) || !Enum.IsDefined(typeof(TechNodeType), kind))
                throw new ArgumentException("Unknown research node kind.", nameof(nodeKind));
            return new TechNodeOptions(kind, costTier, costMultiplier, goldMultiplier, previousId, rootId, requirements, fixedCost?.Native, iconName);
        }
        public TechNodeType NodeType { get; }
        public int CostTier { get; }
        public double CostMultiplier { get; }
        public double GoldMultiplier { get; }
        public string PreviousId { get; }
        public string RootId { get; }
        public IReadOnlyList<string> Requirements { get; }
        readonly CurrencyCollection fixedCost;
        /// <summary>Copy of a fixed cost, or null to use the native cost formula.</summary>
        public CurrencyCollection FixedCost => fixedCost == null ? null : new CurrencyCollection(fixedCost);
        public string IconName { get; }

        public TechNodeOptions(TechNodeType nodeType = TechNodeType.FlowElement, int costTier = 1,
            double costMultiplier = 1, double goldMultiplier = 1, string previousId = null, string rootId = null,
            IEnumerable<string> requirements = null, CurrencyCollection fixedCost = null, string iconName = "Icons_17")
        {
            if (costTier < 1 || costTier > 16) throw new ArgumentOutOfRangeException(nameof(costTier));
            if (!Enum.IsDefined(typeof(TechNodeType), nodeType)) throw new ArgumentOutOfRangeException(nameof(nodeType));
            if (fixedCost != null)
                foreach (var amount in fixedCost.GetCurrency())
                    if (amount.amt < 0) throw new ArgumentOutOfRangeException(nameof(fixedCost), "Research costs cannot be negative.");
            if (double.IsNaN(costMultiplier) || double.IsInfinity(costMultiplier) || costMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(costMultiplier));
            if (double.IsNaN(goldMultiplier) || double.IsInfinity(goldMultiplier) || goldMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(goldMultiplier));
            var ids = requirements == null ? new List<string>() : new List<string>(requirements);
            if (ids.Exists(string.IsNullOrWhiteSpace)) throw new ArgumentException("Requirement IDs cannot be blank.", nameof(requirements));
            if (string.IsNullOrWhiteSpace(iconName)) throw new ArgumentException("Provide a native icon name.", nameof(iconName));
            NodeType = nodeType; CostTier = costTier; CostMultiplier = costMultiplier; GoldMultiplier = goldMultiplier;
            PreviousId = previousId; RootId = rootId; Requirements = ids.AsReadOnly(); this.fixedCost = fixedCost == null ? null : new CurrencyCollection(fixedCost); IconName = iconName;
        }
    }
}
