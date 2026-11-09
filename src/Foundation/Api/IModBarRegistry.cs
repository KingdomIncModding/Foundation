using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>A session-wide registry. Register on Unity's main thread, including before the game scene loads.</summary>
    public interface IModBarRegistry
    {
        IReadOnlyList<IModBarRegistration> Items { get; }
        event Action Changed;
        IModBarRegistration Register(IModBarElement element);
        IModBarRegistration RegisterSeparator(string modId, string id, ModBarAlignment alignment = ModBarAlignment.Left, int order = 0, float width = 16);
        bool TryGet(string modId, string id, out IModBarRegistration registration);
    }
}
