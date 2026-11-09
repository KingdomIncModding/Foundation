using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
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
}
