using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    public sealed class SignalSourceDefinition
    {
        public string ModId { get; }
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<SignalPropertyDefinition> Properties { get; }
        public SignalSourceDefinition(string modId, string id, string name, IEnumerable<SignalPropertyDefinition> properties)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            SignalPropertyDefinition.ValidateId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A signal source needs a display name.", nameof(name));
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            var copy = properties.ToArray();
            if (copy.Any(p => p == null)) throw new ArgumentException("A source cannot contain null properties.", nameof(properties));
            if (copy.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("Property identifiers must be unique inside a source.", nameof(properties));
            ModId = modId; Id = id; Name = name; Properties = Array.AsReadOnly(copy);
        }
    }
}
