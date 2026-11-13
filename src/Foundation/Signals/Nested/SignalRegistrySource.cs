using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class SignalRegistrySource : ISignalSource
    {
        readonly SignalRegistry owner;
        readonly Dictionary<string, ISignalProperty> byId = new Dictionary<string, ISignalProperty>(StringComparer.Ordinal);
        public string ModId { get; }
        public string Id { get; }
        public string Name { get; }
        public bool IsRegistered { get; private set; } = true;
        public IReadOnlyList<ISignalProperty> Properties { get; }

        internal SignalRegistrySource(SignalRegistry owner, SignalSourceDefinition definition)
        {
            this.owner = owner;
            ModId = definition.ModId;
            Id = definition.Id;
            Name = definition.Name;
            var props = new List<ISignalProperty>();
            foreach (var p in definition.Properties)
            {
                global::KingdomFoundation.Api.SignalRegistryProperty prop = new global::KingdomFoundation.Api.SignalRegistryProperty(this, p);
                props.Add(prop);
                byId.Add(prop.Id, prop);
            }

            Properties = props.AsReadOnly();
        }

        public bool TryGetProperty(string id, out ISignalProperty property) => byId.TryGetValue(id ?? "", out property);
        public void Dispose()
        {
            if (!IsRegistered)
                return;
            IsRegistered = false;
            owner.Remove(this);
        }
    }
}
