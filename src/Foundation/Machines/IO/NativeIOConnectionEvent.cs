using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Event after native pipe placement or disconnection (not a ghost preview).</summary>
    public sealed class NativeIOConnectionEvent
    {
        public NativeIOPipeSnapshot Pipe { get; }
        public bool Connected { get; }

        internal NativeIOConnectionEvent(NativeIOPipeSnapshot snapshot, bool connected)
        {
            Pipe = snapshot;
            Connected = connected;
        }
    }
}
