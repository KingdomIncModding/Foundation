using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class ResourceUpgradeOffer
    {
        public double AdditionalQuota { get; }
        public CurrencyCollection Cost { get; }
        public ResourceUpgradeOffer(double amount, CurrencyCollection price)
        { AdditionalQuota = amount; Cost = price; }
    }
}
