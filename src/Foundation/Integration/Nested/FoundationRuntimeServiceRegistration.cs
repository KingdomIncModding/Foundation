using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationRuntimeServiceRegistration : IDisposable
    {
        internal FoundationRuntimeService Owner;
        internal string ModId;
        internal Action<float> Frame;
        internal Key Key;
        internal Action Shortcut;
        internal bool AllowWhileTyping;
        internal bool Active = true;
        public void Dispose() => Owner.Remove(this);
    }
}
