using Assets.Source.Flow;
using System.Runtime.CompilerServices;

namespace KingdomFoundation.Api
{
    /// <summary>Opaque native input/output identity.</summary>
    public sealed class WorkspacePort
    {
        private static readonly ConditionalWeakTable<FlowIO,WorkspacePort> Cache = new ConditionalWeakTable<FlowIO,WorkspacePort>();
        internal static WorkspacePort Wrap(FlowIO native) => native==null ? null : Cache.GetValue(native,value=>new WorkspacePort(value));
        internal FlowIO Native { get; }
        internal WorkspacePort(FlowIO native) { Native=native; }
        public bool IsInput => Native is FlowInput;
        public bool IsOutput => Native is FlowOutput;
        public int ParentIndex => Native.ParentIndex;
        public int Position {get=>Native.Position;set=>Native.Position=value;}
        public UnityEngine.Behaviour World => Native.World;
        public void ClearBuffer()=>Native.BufferedItems.Clear();
        public int BufferedItemCount=>Native.BufferedItems?.Count ?? 0;
        public bool CanBuffer=>Native.CanBuffer;
        public GameInventoryItem AvailableItem=>Native is FlowOutput output ? GameInventoryItem.Wrap(output.AvailableItem) : null;
        public bool CanAcceptItem(GameInventoryItem item)=>Native is FlowInput input && input.CanAcceptItem(item?.Native);
        public WorkspaceObject Parent => Native.Parent==null ? null : GameWorkspaceAccess.Describe(Native.Parent);
        public WorkspacePipe ConnectedPipe => WorkspacePipe.Wrap(Native.ConnectedPipe);
        public void SetConnectedPipe(WorkspacePipe pipe) => Native.ConnectedPipe=pipe?.Native;
        private GameInventoryList buffer;
        public System.Collections.Generic.IReadOnlyList<GameInventoryItem> BufferedItems => buffer ?? (buffer=new GameInventoryList(Native));
        public bool HasBuffer=>Native.HasBuffer;
        public bool InputItem(GameInventoryItem item)=>Native is FlowInput input && input.InputItem(item?.Native);
        public GameInventoryItem ConsumeItem()=>GameInventoryItem.Wrap(Native.ConsumeItem());
        public void PrependBuffer(GameInventoryItem item)=>Native.BufferedItems.Insert(0,item.Native);
        public void SetBuffer(System.Collections.Generic.IEnumerable<GameInventoryItem> items)
        {var values=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(items,item=>item.Native));Native.BufferedItems.Clear();Native.BufferedItems.AddRange(values);}
        public override bool Equals(object obj) => obj is WorkspacePort other && ReferenceEquals(Native,other.Native);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Native);
        public static bool operator ==(WorkspacePort left,WorkspacePort right) => ReferenceEquals(left,right) || (!ReferenceEquals(left,null) && left.Equals(right));
        public static bool operator !=(WorkspacePort left,WorkspacePort right) => !(left==right);
    }
}
