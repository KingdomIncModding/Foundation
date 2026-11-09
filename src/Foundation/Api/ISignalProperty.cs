using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    public interface ISignalProperty
    {
        string Id { get; }
        string Name { get; }
        Type ValueType { get; }
        bool CanWrite { get; }
        object Read();
        void Write(object value);
        event Action<SignalPropertyChanged> Changed;
    }
}
