using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public sealed class FlowPipeMetrics
    {
        public long TotalTransfers { get; }
        public int TransfersLastTenSeconds { get; }
        public double AverageTransfersPerSecond { get; }
        public int MeasurementWindowSeconds => 10;
        internal FlowPipeMetrics(long total, int recent)
        { TotalTransfers = total; TransfersLastTenSeconds = recent; AverageTransfersPerSecond = recent / 10.0; }
    }
}
