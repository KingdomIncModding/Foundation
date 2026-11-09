using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Global registry for mod configuration sections displayed in Foundation's custom Config menu.</summary>
    public interface IModConfigRegistry
    {
        IReadOnlyList<IModConfigSection> Sections { get; }
        event Action Changed;
        IModConfigSection GetOrCreateSection(string modId, string modName = null, string description = null);
        bool TryGetSection(string modId, out IModConfigSection section);
        void RemoveSection(string modId);
    }
}
