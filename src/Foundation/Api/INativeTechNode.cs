using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Bridge to the game's real research model, for integration with other native systems.</summary>
    public interface INativeTechNode : ITechNode
    {
        TechNode NativeNode { get; }
    }
}
