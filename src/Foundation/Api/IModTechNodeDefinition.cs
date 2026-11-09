using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Optional explicit category. Otherwise Foundation detects the registering BepInEx plugin.</summary>
    public interface IModTechNodeDefinition : ITechNodeDefinition
    {
        string ModId { get; }
        string ModName { get; }
    }
}
