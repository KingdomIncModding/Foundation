using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Optional native research settings for a definition.</summary>
    public interface INativeTechNodeDefinition : ITechNodeDefinition
    {
        TechNodeOptions Research { get; }
    }
}
