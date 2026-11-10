using System;
using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Immutable workspace metadata. Register the required research first.</summary>
    public sealed class WorkspaceNodeDefinition : IWorkspaceNodeDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string RequiredTechId { get; }
        public ElementCategory Category { get; }
        public Vector2Int Size { get; }
        /// <summary>Gets or sets the icon name value.</summary>
        public string IconName { get; }
        public Sprite Icon { get; }
        public double CostMultiplier { get; }
        public double GoldMultiplier { get; }
        public Func<WorkspaceNode> Factory { get; }
        readonly CurrencyCollection cost;
        public CurrencyCollection FixedCost => cost == null ? null : new CurrencyCollection(cost);

        public WorkspaceNodeDefinition(string id, string title, Func<WorkspaceNode> factory, string requiredTechId,
            Vector2Int size = default, string description = "", ElementCategory category = ElementCategory.Utility,
            string iconName = "Icons_104", Sprite icon = null, double costMultiplier = 1, double goldMultiplier = 1,
            CurrencyCollection fixedCost = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("Use a stable, nonempty type ID without surrounding whitespace.", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A workspace node needs a title.", nameof(title));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(requiredTechId)) throw new ArgumentException("Register or select a required technology first.", nameof(requiredTechId));
            if (size == Vector2Int.zero) size = new Vector2Int(256, 128);
            if (size.x < 64 || size.y < 64 || size.x > 16384 || size.y > 16384) throw new ArgumentOutOfRangeException(nameof(size));
            if (!Enum.IsDefined(typeof(ElementCategory), category) || category == ElementCategory.None) throw new ArgumentOutOfRangeException(nameof(category));
            if (string.IsNullOrWhiteSpace(iconName)) throw new ArgumentException("Provide a native fallback icon name.", nameof(iconName));
            if (double.IsNaN(costMultiplier) || double.IsInfinity(costMultiplier) || costMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(costMultiplier));
            if (double.IsNaN(goldMultiplier) || double.IsInfinity(goldMultiplier) || goldMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(goldMultiplier));
            if (fixedCost != null) foreach (var amount in fixedCost.GetCurrency()) if (amount.amt < 0) throw new ArgumentOutOfRangeException(nameof(fixedCost));
            Id = id; Title = title; Factory = factory; RequiredTechId = requiredTechId; Size = size; Description = description ?? "";
            Category = category; IconName = iconName; Icon = icon; CostMultiplier = costMultiplier; GoldMultiplier = goldMultiplier;
            cost = fixedCost == null ? null : new CurrencyCollection(fixedCost);
        }
        internal static WorkspaceNodeDefinition Snapshot(IWorkspaceNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return new WorkspaceNodeDefinition(definition.Id, definition.Title, definition.Factory, definition.RequiredTechId, definition.Size,
                definition.Description, definition.Category, definition.IconName, definition.Icon, definition.CostMultiplier, definition.GoldMultiplier, definition.FixedCost);
        }
    }
}
