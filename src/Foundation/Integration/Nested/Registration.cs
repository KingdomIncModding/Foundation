using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    internal sealed partial class FoundationRuntimeService
    {
        sealed class Registration : IDisposable
        {
            internal FoundationRuntimeService Owner;
            internal string ModId;
            internal Action<float> Frame;
            internal Key Key;
            internal Action Shortcut;
            internal bool AllowWhileTyping;
            internal bool Active = true;
            /// <summary>Performs the dispose operation.</summary>
            public void Dispose() => Owner.Remove(this);
        }
    }

}
