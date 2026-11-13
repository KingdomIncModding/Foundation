using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Measured pipe events aggregated by direction. Not a machine capacity.</summary>
    public sealed class NativeIOMachineThroughputSnapshot
    {
        public string MachineGuid { get; }
        public int FlowIndex { get; }
        public long ObservedInputItems { get; }
        public long ObservedOutputItems { get; }
        public double ObservedInputItemsPerSecond { get; }
        public double ObservedOutputItemsPerSecond { get; }
        /// <summary>Native production field when applicable, distinct from actual transfer observations.</summary>
        public float? NativeProductionOutputPerSecond { get; }
        public int ObservationWindowSeconds => 10;

        internal NativeIOMachineThroughputSnapshot(string guid, int flow, long incoming, long outgoing, double incomingRate, double outgoingRate, float? production)
        {
            MachineGuid = guid;
            FlowIndex = flow;
            ObservedInputItems = incoming;
            ObservedOutputItems = outgoing;
            ObservedInputItemsPerSecond = incomingRate;
            ObservedOutputItemsPerSecond = outgoingRate;
            NativeProductionOutputPerSecond = production;
        }
    }
}
