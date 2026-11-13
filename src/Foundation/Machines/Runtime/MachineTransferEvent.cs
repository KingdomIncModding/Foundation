using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    public sealed class MachineTransferEvent
    {
        public string SourceGuid { get; }
        public string DestinationGuid { get; }
        public long TotalObservedItems { get; }
        public double ObservedItemsPerSecond { get; }

        internal MachineTransferEvent(string source, string destination, long count, double rate)
        {
            SourceGuid = source;
            DestinationGuid = destination;
            TotalObservedItems = count;
            ObservedItemsPerSecond = rate;
        }
    }
}
