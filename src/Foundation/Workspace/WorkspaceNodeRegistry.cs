using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class WorkspaceNodeRegistry : IWorkspaceNodeRegistry
    {
        internal static readonly WorkspaceNodeRegistry Shared = new WorkspaceNodeRegistry();
        readonly Dictionary<string, WorkspaceNodeType> types = new Dictionary<string, WorkspaceNodeType>(StringComparer.Ordinal);
        /// <summary>Gets or sets the nodes value.</summary>
        public IReadOnlyList<IWorkspaceNodeType> Nodes => Array.AsReadOnly(types.Values.Where(n => n.IsRegistered).Cast<IWorkspaceNodeType>().ToArray());
        public event Action Changed;
        public IWorkspaceNodeType RegisterWorkspaceNode(IWorkspaceNodeDefinition definition)
        {
            var snapshot = WorkspaceNodeDefinition.Snapshot(definition);
            // The existing Registry remains authoritative for mod-created node
            // registrations; never register the same ID in a second B6 catalog.
            if (types.TryGetValue(snapshot.Id, out var previous) && previous.IsRegistered) throw new ArgumentException("Workspace type already registered: " + snapshot.Id);
            if (typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements." + snapshot.Id) != null) throw new ArgumentException("Cannot replace a native building type: " + snapshot.Id);
            var tech = FoundationApi.Progression.FindTech(snapshot.RequiredTechId);
            if (tech == null || (NativeResearch.Owns(tech) && !NativeResearch.Active(tech))) throw new ArgumentException("Unknown or inactive required research: " + snapshot.RequiredTechId);
            if (!snapshot.Icon && !FoundationApi.Presentation.FindSprite(snapshot.IconName))
                SpriteLibrary.Get(snapshot.IconName); // Preserve vanilla fallback/throw behavior.
            var registration = new WorkspaceNodeType(this, snapshot);
            types[snapshot.Id] = registration;
            try { registration.Preview = registration.CreateModel(); }
            catch { if (previous == null) types.Remove(snapshot.Id); else types[snapshot.Id] = previous; throw; }
            Notify(); return registration;
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string id, out IWorkspaceNodeType nodeType)
        {
            if (id != null && types.TryGetValue(id, out var found) && found.IsRegistered) { nodeType = found; return true; }
            nodeType = null; return false;
        }
        internal WorkspaceNodeType Resolve(string id)
        {
            if (id == null || !types.TryGetValue(id, out var found)) throw new InvalidOperationException("Register the workspace type before constructing its models: " + id);
            return found;
        }
        internal bool TryResolve(string id, out WorkspaceNodeType registration) { registration = null; return id != null && types.TryGetValue(id, out registration); }
        internal IEnumerable<FlowElement> Include(IEnumerable<FlowElement> originals)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var original in originals) { used.Add(original.ElementType); yield return original; }
            foreach (WorkspaceNodeType type in Nodes)
            {
                if (!used.Add(type.Id)) throw new InvalidOperationException("Another plugin also provides workspace type: " + type.Id);
                yield return type.Preview;
            }
        }
        internal void Notify()
        {
            if (Changed == null) return;
            foreach (Action listener in Changed.GetInvocationList()) try { listener(); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
