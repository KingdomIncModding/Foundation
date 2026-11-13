using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Machines
{
    public enum MachineChangeKind
    {
        Added,
        Removed,
        SettingsChanged,
        PortsChanged,
        MovedFlow
    }
}
