using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed partial class SignalRegistry
    {
        sealed class Source : ISignalSource
        {
            readonly SignalRegistry owner;
            readonly Dictionary<string, ISignalProperty> byId = new Dictionary<string, ISignalProperty>(StringComparer.Ordinal);
            /// <summary>Gets or sets the mod id value.</summary>
            public string ModId { get; }
            public string Id { get; }
            public string Name { get; }
            public bool IsRegistered { get; private set; } = true;
            public IReadOnlyList<ISignalProperty> Properties { get; }
            internal Source(SignalRegistry owner, SignalSourceDefinition definition)
            {
                this.owner = owner; ModId = definition.ModId; Id = definition.Id; Name = definition.Name;
                var props = new List<ISignalProperty>();
                foreach (var p in definition.Properties) { var prop = new Property(this, p); props.Add(prop); byId.Add(prop.Id, prop); }
                Properties = props.AsReadOnly();
            }
            /// <summary>Attempts to get property.</summary>
            public bool TryGetProperty(string id, out ISignalProperty property) => byId.TryGetValue(id ?? "", out property);
            public void Dispose() { if (!IsRegistered) return; IsRegistered = false; owner.Remove(this); }
        }
    }

}
