using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
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
