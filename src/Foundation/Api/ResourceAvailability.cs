using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class ResourceAvailability
    {
        public ResourceItemType Resource { get; }
        public double Available { get; }
        public double Demand { get; }
        public float QuotaMultiplier { get; }
        public ResourceAvailability(ResourceItemType type, double available, double demand, float quotaMultiplier)
        { Resource = type; Available = available; Demand = demand; QuotaMultiplier = quotaMultiplier; }
    }
}
