using Assets.Source.Flow;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class HostedWorkspaceNode : WorkspaceNode
    {
        internal FrameworkWorkspaceNode Owner {get;}
        internal HostedWorkspaceNode(FrameworkWorkspaceNode owner,string typeId):base(typeId){Owner=owner;}
        public override void Update(float delta)=>Owner.Update(delta);
        public override bool HandleReopenBuyMenu()=>Owner.HandleReopenBuyMenu();
        protected override void DataToJson(JsonObject data)=>Owner.SaveData(KingdomFoundation.Json.JsonObject.Wrap(data));
        protected override void DataFromJson(JsonObject data,FlowDiagram parent)=>Owner.LoadData(KingdomFoundation.Json.JsonObject.Wrap(data),GameWorkspace.Wrap(parent));
        protected override void CopySettingsTo(FlowElement copy)=>Owner.CopyData(((HostedWorkspaceNode)copy).Owner);
    }
}
