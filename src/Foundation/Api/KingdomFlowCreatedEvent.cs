using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the kingdom flow created event API.</summary>
    public sealed class KingdomFlowCreatedEvent
    {
        public FlowDiagram Flow { get; }
        public int Index { get; }
        public KingdomFlowCreatedEvent(FlowDiagram flow, int index) { Flow = flow; Index = index; }
    }
}
