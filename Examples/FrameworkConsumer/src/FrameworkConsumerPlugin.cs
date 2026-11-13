using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;

namespace FoundationFrameworkExample
{
    [BepInPlugin(Id,"Foundation Framework Example","1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation","0.14.0")]
    public sealed class FrameworkConsumerPlugin : BaseUnityPlugin
    {
        public const string Id="example.foundation.framework";
        private FoundationModContext context;
        private void Awake()
        {
            context=FoundationApi.ForMod(Id);
            context.Register(new TechNodeDefinition("framework_counter","Counter",Id,"Framework Example",Vector2.zero,
                "Example machine with mod-owned behavior and Foundation-owned native integration.",
                research:TechNodeOptions.Create(new GameMoney("Gold",100),requirements:new[]{"accountant"})));
            context.Register(new FrameworkWorkspaceNodeDefinition("FrameworkCounter","Counter",()=>new CounterNode(),"framework_counter",
                size:new Vector2Int(256,128),description:"Counts elapsed simulation time. Its behavior and saved data belong to this example mod."));
        }
        private void OnDestroy()=>context?.Dispose();
    }
}
