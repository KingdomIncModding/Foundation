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
}
