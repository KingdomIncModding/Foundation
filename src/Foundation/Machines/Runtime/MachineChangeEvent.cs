using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Machines
{
    public sealed class MachineChangeEvent
    {
        public MachineChangeKind Kind { get; }
        public MachineIdentity Identity { get; }
        public string NativeType { get; }

        internal MachineChangeEvent(MachineChangeKind kind, MachineIdentity identity, string nativeType)
        {
            Kind = kind;
            Identity = identity;
            NativeType = nativeType;
        }
    }
}
