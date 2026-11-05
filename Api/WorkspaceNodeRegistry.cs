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
        public IReadOnlyList<IWorkspaceNodeType> Nodes => Array.AsReadOnly(types.Values.Where(n => n.IsRegistered).Cast<IWorkspaceNodeType>().ToArray());
        public event Action Changed;
        public IWorkspaceNodeType RegisterWorkspaceNode(IWorkspaceNodeDefinition definition)
        {
            var snapshot = WorkspaceNodeDefinition.Snapshot(definition);
            if (types.TryGetValue(snapshot.Id, out var previous) && previous.IsRegistered) throw new ArgumentException("Workspace type already registered: " + snapshot.Id);
            if (typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements." + snapshot.Id) != null) throw new ArgumentException("Cannot replace a native building type: " + snapshot.Id);
            var tech = TechNode.Get(snapshot.RequiredTechId);
            if (tech == null || (NativeResearch.Owns(tech) && !NativeResearch.Active(tech))) throw new ArgumentException("Unknown or inactive required research: " + snapshot.RequiredTechId);
            if (!snapshot.Icon) SpriteLibrary.Get(snapshot.IconName);
            var registration = new WorkspaceNodeType(this, snapshot);
            types[snapshot.Id] = registration;
            try { registration.Preview = registration.CreateModel(); }
            catch { if (previous == null) types.Remove(snapshot.Id); else types[snapshot.Id] = previous; throw; }
            Notify(); return registration;
        }
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
    internal sealed class WorkspaceNodeType : IWorkspaceNodeType
    {
        readonly WorkspaceNodeRegistry owner;
        readonly ConditionalWeakTable<WorkspaceNode, object> instances = new ConditionalWeakTable<WorkspaceNode, object>();
        Type modelType;
        public string Id => Definition.Id;
        public IWorkspaceNodeDefinition Definition { get; }
        public bool IsRegistered { get; private set; } = true;
        internal WorkspaceNode Preview;
        internal WorkspaceNodeType(WorkspaceNodeRegistry owner, WorkspaceNodeDefinition definition) { this.owner = owner; Definition = definition; }
        internal WorkspaceNode CreateModel()
        {
            var node = Definition.Factory();
            if (node == null || node.ElementType != Id || node.World || node.Size != Definition.Size)
                throw new InvalidOperationException("Factory must create a fresh unplaced WorkspaceNode with the registered ID and size: " + Id);
            if (instances.TryGetValue(node, out _) || (modelType != null && node.GetType() != modelType)) throw new InvalidOperationException("Factory must return fresh models of one stable type: " + Id);
            for (int i = 0; i < node.Inputs.Count; i++) ValidatePort(node, node.Inputs[i], i);
            for (int i = 0; i < node.Outputs.Count; i++) ValidatePort(node, node.Outputs[i], i);
            modelType = node.GetType(); instances.Add(node, new object()); return node;
        }
        static void ValidatePort(WorkspaceNode node, FlowIO port, int index)
        {
            if (port == null || !ReferenceEquals(port.Parent, node) || port.ParentIndex != index || port.BufferSize < 0 || port.Position == 0 || Math.Abs((long)port.Position) >= node.Size.y)
                throw new InvalidOperationException("Use native ports with matching parent/index, nonnegative capacity and a position within the node height.");
        }
        public WorkspaceNode CreateInstance()
        { if (!IsRegistered) throw new ObjectDisposedException(nameof(IWorkspaceNodeType)); return CreateModel(); }
        public WorldFlowGhost BeginPlacement(bool hold = false)
        {
            if (!IsRegistered) throw new ObjectDisposedException(nameof(IWorkspaceNodeType));
            if (!WorldFlowManager.Instance || GamePlayer.Current == null) throw new InvalidOperationException("Open a kingdom before starting placement.");
            if (!Preview.IsPurchaseable(GamePlayer.Current)) throw new InvalidOperationException("Research must be unlocked before placement: " + Definition.RequiredTechId);
            if (!GamePlayer.Current.CanAfford(Preview.GetPurchaseCost())) throw new InvalidOperationException("Insufficient currency for this building.");
            return WorldFlowManager.Instance.ShowPurchaseBlueprint(Preview, hold);
        }
        public void Dispose() { if (!IsRegistered) return; IsRegistered = false; owner.Notify(); }
    }
}
