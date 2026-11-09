using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    public sealed class SignalPropertyChanged
    {
        public string SourceId { get; }
        public string PropertyId { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public SignalPropertyChanged(string sourceId, string propertyId, object oldValue, object newValue)
        { SourceId = sourceId; PropertyId = propertyId; OldValue = oldValue; NewValue = newValue; }
    }
}
