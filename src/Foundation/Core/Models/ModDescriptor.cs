using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    public sealed class ModDescriptor
    {
        public string Id { get; }
        public string Name { get; }
        public string Author { get; }
        public string Description { get; }
        public Version Version { get; }
        public Version MinimumCoreApiVersion { get; }
        public IReadOnlyList<ModDependency> Dependencies { get; }

        public ModDescriptor(string id, string name, Version version, string author = "",
            string description = "", IEnumerable<ModDependency> dependencies = null,
            Version minimumCoreApiVersion = null)
        {
            ModIdentity.Validate(id);
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
            Id = id;
            Name = name;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Author = author ?? string.Empty;
            Description = description ?? string.Empty;
            MinimumCoreApiVersion = minimumCoreApiVersion;
            var entries = dependencies?.ToArray() ?? Array.Empty<ModDependency>();
            if (entries.Any(x => x == null)) throw new ArgumentException("Null dependency.", nameof(dependencies));
            if (entries.GroupBy(x => x.Kind.ToString() + ":" + x.Id, StringComparer.Ordinal)
                .Any(group => group.Count() > 1)) throw new ArgumentException("Duplicate dependencies.", nameof(dependencies));
            if (entries.Any(x => x.Kind == DependencyKind.FoundationModule && x.Id == id))
                throw new ArgumentException("A module cannot depend on itself.", nameof(dependencies));
            Dependencies = Array.AsReadOnly(entries);
        }
    }
}
