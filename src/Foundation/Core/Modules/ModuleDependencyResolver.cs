using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Core
{
    internal sealed class ModuleDependencyResolver
    {
        readonly ICorePluginCatalog plugins;
        internal ModuleDependencyResolver(ICorePluginCatalog plugins) { this.plugins = plugins; }
        internal string PendingReason(ModDescriptor descriptor, IReadOnlyDictionary<string, ModRegistration> modules)
        {
            foreach (var dep in descriptor.Dependencies)
            {
                if (dep.Kind == DependencyKind.BepInExPlugin)
                {
                    if (!plugins.Has(dep.Id, dep.MinimumVersion) && !dep.Optional)
                        return "Requires BepInEx plugin '" + dep.Id + "' " + dep.MinimumVersion;
                    continue;
                }
                if (!modules.TryGetValue(dep.Id, out var other))
                {
                    if (!dep.Optional) return "Waiting for Foundation module '" + dep.Id + "'.";
                    continue;
                }
                if (dep.Optional && (other.State != ModState.Active ||
                    (dep.MinimumVersion != null && other.Descriptor.Version < dep.MinimumVersion))) continue;
                if (other.State == ModState.Failed || other.State == ModState.Disposed || other.State == ModState.Stopped)
                    return "Required Foundation module '" + dep.Id + "' is " + other.State + ".";
                if (dep.MinimumVersion != null && other.Descriptor.Version < dep.MinimumVersion)
                    return "Module '" + dep.Id + "' version " + other.Descriptor.Version +
                        " does not meet " + dep.MinimumVersion + ".";
                if (other.State != ModState.Active) return "Waiting for module '" + dep.Id + "' to activate.";
            }
            return null;
        }
        internal string DetectCycle(string startId, IReadOnlyDictionary<string, ModRegistration> modules)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            return Visit(startId, modules, visiting, visited);
        }
        string Visit(string id, IReadOnlyDictionary<string, ModRegistration> modules,
            HashSet<string> visiting, HashSet<string> visited)
        {
            if (visiting.Contains(id)) return "Dependency cycle includes '" + id + "'.";
            if (visited.Contains(id)) return null;
            if (!modules.TryGetValue(id, out var module)) return null;
            visiting.Add(id);
            foreach (var dependency in module.Descriptor.Dependencies.Where(x =>
                !x.Optional && x.Kind == DependencyKind.FoundationModule))
            {
                var cycle = Visit(dependency.Id, modules, visiting, visited);
                if (cycle != null) return cycle;
            }
            visiting.Remove(id);
            visited.Add(id);
            return null;
        }
    }
}
