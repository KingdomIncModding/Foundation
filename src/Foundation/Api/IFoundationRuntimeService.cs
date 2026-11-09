using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    /// <summary>Frame events and mod-owned keyboard shortcuts managed by Foundation.</summary>
    public interface IFoundationRuntimeService
    {
        IDisposable EachFrame(string modId, Action<float> callback);
        IDisposable KeyPressed(string modId, Key key, Action callback, bool allowWhileTyping = false);
        IReadOnlyDictionary<string, int> RegistrationCounts { get; }
    }
}
