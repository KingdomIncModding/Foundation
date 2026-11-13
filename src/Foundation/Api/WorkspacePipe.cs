using Assets.Source.Flow;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace KingdomFoundation.Api
{
    /// <summary>Opaque native pipe identity and connection/view access.</summary>
    public sealed class WorkspacePipe
    {
        private static readonly ConditionalWeakTable<FlowPipe,WorkspacePipe> Cache = new ConditionalWeakTable<FlowPipe,WorkspacePipe>();
        internal static WorkspacePipe Wrap(FlowPipe native) => native==null ? null : Cache.GetValue(native,value=>new WorkspacePipe(value));
        internal FlowPipe Native { get; }
        internal WorkspacePipe(FlowPipe native) { Native=native; }
        public WorkspacePort ConnectedInput => WorkspacePort.Wrap(Native.ConnectedInput);
        public bool ItemRejected=>Native.ItemRejected;
        public WorkspacePort ConnectedOutput => WorkspacePort.Wrap(Native.ConnectedOutput);
        public UnityEngine.Behaviour World => Native.World;
        public System.Collections.Generic.IReadOnlyList<Vector2Int> Nodes => Native.Nodes.ToArray();
        public void SetNodes(System.Collections.Generic.IEnumerable<Vector2Int> nodes) { var values=System.Linq.Enumerable.ToArray(nodes);Native.Nodes.Clear();Native.Nodes.AddRange(values); }
        public void Connect(WorkspacePort output,WorkspacePort input) => Native.Connect((FlowOutput)output?.Native,(FlowInput)input?.Native);
        public void ClearViewReference() => Native.World=null;
        public override bool Equals(object obj) => obj is WorkspacePipe other && ReferenceEquals(Native,other.Native);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Native);
        public static bool operator ==(WorkspacePipe left,WorkspacePipe right) => ReferenceEquals(left,right) || (!ReferenceEquals(left,null) && left.Equals(right));
        public static bool operator !=(WorkspacePipe left,WorkspacePipe right) => !(left==right);
    }
}
