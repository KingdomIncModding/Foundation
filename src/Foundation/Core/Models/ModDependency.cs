using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the mod dependency API.</summary>
    public sealed class ModDependency
    {
        public string Id { get; }
        public DependencyKind Kind { get; }
        public Version MinimumVersion { get; }
        public bool Optional { get; }

        public ModDependency(string id, DependencyKind kind = DependencyKind.FoundationModule,
            Version minimumVersion = null, bool optional = false)
        {
            ModIdentity.Validate(id);
            if (!Enum.IsDefined(typeof(DependencyKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id;
            Kind = kind;
            MinimumVersion = minimumVersion;
            Optional = optional;
        }
    }
}
