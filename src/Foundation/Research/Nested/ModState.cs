using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Provides the tech tree tabs API.</summary>
    public sealed partial class TechTreeTabs
    {
        sealed class ModState { internal TechNode Root; internal CameraState Camera; }
    }

}
