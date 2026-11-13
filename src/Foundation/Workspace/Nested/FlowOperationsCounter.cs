using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class FlowOperationsCounter
    {
        internal readonly int[] BucketSeconds = new int[10];
        internal readonly int[] BucketCounts = new int[10];
        internal long Total;
        public FlowOperationsCounter()
        {
            for (int i = 0; i < BucketSeconds.Length; i++)
                BucketSeconds[i] = int.MinValue;
        }
    }
}
