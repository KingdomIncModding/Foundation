using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the isignal property API.</summary>
    public interface ISignalProperty
    {
        string Id { get; }
        string Name { get; }
        Type ValueType { get; }
        bool CanWrite { get; }
        object Read();
        void Write(object value);
        /// <summary>Occurs when changed is raised.</summary>
        event Action<SignalPropertyChanged> Changed;
    }
}
