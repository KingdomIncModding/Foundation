using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Convenient immutable implementation; custom ITechNodeDefinition classes also work.</summary>
    public sealed class TechNodeDefinition : INativeTechNodeDefinition, IModTechNodeDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public Vector2 Position { get; }
        public TechNodeOptions Research { get; }
        /// <summary>Gets or sets the mod id value.</summary>
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
