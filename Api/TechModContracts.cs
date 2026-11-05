using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Optional explicit category. Otherwise Foundation detects the registering BepInEx plugin.</summary>
    public interface IModTechNodeDefinition : ITechNodeDefinition
    {
        string ModId { get; }
        string ModName { get; }
    }

    /// <summary>A mod's category in the research sidebar. Empty categories are omitted from the registry.</summary>
    public interface ITechMod
    {
        string Id { get; }
        string Name { get; }
        IReadOnlyList<ITechNode> Nodes { get; }
    }

    public interface IModTechNode : ITechNode
    {
        ITechMod Mod { get; }
    }

    /// <summary>Additional category access without changing the original registry interface.</summary>
    public interface IModTechNodeRegistry : ITechNodeRegistry
    {
        IReadOnlyList<ITechMod> Mods { get; }
    }

    internal sealed class TechMod : ITechMod
    {
        readonly TechNodeRegistry registry;
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
