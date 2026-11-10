using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    internal sealed class TechMod : ITechMod
    {
        readonly TechNodeRegistry registry;
        /// <summary>Gets or sets the id value.</summary>
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<ITechNode> Nodes => registry.NodesFor(Id);
        internal TechMod(TechNodeRegistry registry, string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("Provide a stable mod ID.");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Provide a mod display name.");
            this.registry = registry; Id = id; Name = name;
        }
    }
}
