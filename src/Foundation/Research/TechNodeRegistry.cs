using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace KingdomFoundation.Api
{
    /// <summary>Owns registrations and protects existing nodes from identifier collisions.</summary>
    public sealed class TechNodeRegistry : IModTechNodeRegistry
    {
        readonly Dictionary<string, TechNodeRegistration> byId = new Dictionary<string, TechNodeRegistration>(StringComparer.Ordinal);
        readonly List<ITechNode> ordered = new List<ITechNode>();
        readonly Dictionary<string, TechMod> mods = new Dictionary<string, TechMod>(StringComparer.Ordinal);
        readonly bool native;
        public TechNodeRegistry() { }
        internal TechNodeRegistry(bool native) { this.native = native; }
        /// <summary>Gets or sets the nodes value.</summary>
        public IReadOnlyList<ITechNode> Nodes => Array.AsReadOnly(ordered.ToArray());
        public IReadOnlyList<ITechMod> Mods => Array.AsReadOnly(mods.Values.Where(m => m.Nodes.Count > 0)
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Id, StringComparer.Ordinal).Cast<ITechMod>().ToArray());
        internal IReadOnlyList<ITechNode> NodesFor(string modId) => Array.AsReadOnly(ordered.Where(n => ((IModTechNode)n).Mod.Id == modId).ToArray());
        public event Action Changed;

        public ITechNode CreateTechNode(ITechNodeDefinition definition)
        {
            var snapshot = TechNodeDefinition.Snapshot(definition);
            if (byId.ContainsKey(snapshot.Id)) throw new ArgumentException("A tech node with identifier '" + snapshot.Id + "' is already registered.", nameof(definition));
            var identity = snapshot.ModId == null ? TechModIdentity.Detect() : (snapshot.ModId, snapshot.ModName);
            if (!mods.TryGetValue(identity.Item1, out var mod))
                mods.Add(identity.Item1, mod = new TechMod(this, identity.Item1, identity.Item2));
            else if (snapshot.ModId != null && mod.Name != snapshot.ModName) throw new ArgumentException("This mod ID already has a different display name.");
            var registration = new TechNodeRegistration(this, snapshot, mod);
            if (native) registration.NativeNode = NativeResearch.Register(registration, snapshot);
            byId.Add(registration.Id, registration); ordered.Add(registration); Notify();
            return registration;
        }

        /// <summary>Attempts to get.</summary>
        public bool TryGet(string id, out ITechNode node)
        {
            if (id != null && byId.TryGetValue(id, out var found)) { node = found; return true; }
            node = null; return false;
        }

        internal void Remove(TechNodeRegistration node)
        {
            if (!byId.TryGetValue(node.Id, out var current) || !ReferenceEquals(node, current)) return;
            byId.Remove(node.Id); ordered.Remove(node); Notify();
        }
        internal void Notify()
        {
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
