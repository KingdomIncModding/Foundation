using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
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
}
