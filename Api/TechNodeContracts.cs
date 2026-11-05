using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Data used to create or update a node on the Mods tech page.</summary>
    public interface ITechNodeDefinition
    {
        /// <summary>Stable, globally unique identifier. Prefix it with your mod's GUID.</summary>
        string Id { get; }
        string Title { get; }
        string Description { get; }
        /// <summary>Optional caller-owned sprite. Keep it alive for the registration's lifetime.</summary>
        Sprite Icon { get; }
        /// <summary>Native research-map coordinates: X right, Y up.</summary>
        Vector2 Position { get; }
    }

    /// <summary>A live registration, independent of the lifetime of a game UI scene.</summary>
    public interface ITechNode : IDisposable
    {
        string Id { get; }
        ITechNodeDefinition Definition { get; }
        bool IsRegistered { get; }
        /// <summary>Raised after native research is successfully unlocked.</summary>
        event Action<ITechNode> Clicked;
        /// <summary>Replace the presentation data. The identifier must stay the same.</summary>
        void Update(ITechNodeDefinition definition);
    }

    /// <summary>Optional native research settings for a definition.</summary>
    public interface INativeTechNodeDefinition : ITechNodeDefinition
    {
        TechNodeOptions Research { get; }
    }

    /// <summary>Bridge to the game's real research model, for integration with other native systems.</summary>
    public interface INativeTechNode : ITechNode
    {
        TechNode NativeNode { get; }
    }

    /// <summary>Immutable native research rules. Register parent nodes before their children.</summary>
    public sealed class TechNodeOptions
    {
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

    /// <summary>Register on Unity's main thread, normally from a dependent plugin's Awake.</summary>
    public interface ITechNodeRegistry
    {
        /// <summary>Snapshot of current registrations, ordered by creation.</summary>
        IReadOnlyList<ITechNode> Nodes { get; }
        /// <summary>Raised after create, update and disposal. UI changes appear by the next frame.</summary>
        event Action Changed;
        /// <summary>Create a node. Duplicate identifiers are rejected without replacing an existing node.</summary>
        ITechNode CreateTechNode(ITechNodeDefinition definition);
        bool TryGet(string id, out ITechNode node);
    }

    /// <summary>Convenient immutable implementation; custom ITechNodeDefinition classes also work.</summary>
    public sealed class TechNodeDefinition : INativeTechNodeDefinition, IModTechNodeDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public Vector2 Position { get; }
        public TechNodeOptions Research { get; }
        public string ModId { get; }
        public string ModName { get; }

        public TechNodeDefinition(string id, string title, Vector2 position = default, string description = "", Sprite icon = null, TechNodeOptions research = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("Use a nonempty identifier without leading or trailing whitespace.", nameof(id));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A node needs a title.", nameof(title));
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y))
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be finite.");
            if (Math.Abs(position.x) > 1000000 || Math.Abs(position.y) > 1000000) throw new ArgumentOutOfRangeException(nameof(position), "Use native map coordinates within one million units.");
            Id = id; Title = title; Position = position; Description = description ?? ""; Icon = icon; Research = research ?? new TechNodeOptions();
        }

        internal static TechNodeDefinition Snapshot(ITechNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is IModTechNodeDefinition mod && (mod.ModId != null || mod.ModName != null))
                return new TechNodeDefinition(definition.Id, definition.Title, mod.ModId, mod.ModName, definition.Position, definition.Description, definition.Icon, (definition as INativeTechNodeDefinition)?.Research);
            return new TechNodeDefinition(definition.Id, definition.Title, definition.Position, definition.Description, definition.Icon, (definition as INativeTechNodeDefinition)?.Research);
        }

        /// <summary>Explicit mod category for registrations made through shared helpers. Existing constructor remains binary compatible.</summary>
        public TechNodeDefinition(string id, string title, string modId, string modName, Vector2 position = default,
            string description = "", Sprite icon = null, TechNodeOptions research = null) : this(id, title, position, description, icon, research)
        {
            if (string.IsNullOrWhiteSpace(modId) || modId != modId.Trim()) throw new ArgumentException("Provide a stable mod ID.", nameof(modId));
            if (string.IsNullOrWhiteSpace(modName)) throw new ArgumentException("Provide a mod display name.", nameof(modName));
            ModId = modId; ModName = modName;
        }
    }
}
