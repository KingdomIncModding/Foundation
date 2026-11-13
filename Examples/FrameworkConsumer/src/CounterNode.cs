using KingdomFoundation.Api;
using KingdomFoundation.Json;

namespace FoundationFrameworkExample
{
    public sealed class CounterNode : FrameworkWorkspaceNode
    {
        public double ElapsedSeconds {get;private set;}
        public CounterNode():base("FrameworkCounter"){}
        public override void Update(float delta)=>ElapsedSeconds+=delta;
        protected override void DataToJson(JsonObject data)=>data["CounterSeconds"]=ElapsedSeconds;
        protected override void DataFromJson(JsonObject data,GameWorkspace parent)=>ElapsedSeconds=data["CounterSeconds"].AsNumber;
        protected override void CopySettingsTo(FrameworkWorkspaceNode copy)=>((CounterNode)copy).ElapsedSeconds=ElapsedSeconds;
    }
}
