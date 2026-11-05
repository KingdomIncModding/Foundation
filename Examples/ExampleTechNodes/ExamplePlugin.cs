using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;
using Assets.Source.Player;
using Assets.Source.Flow;
using LightJson;

namespace FoundationExamples
{
    // Reference Foundation.dll in your project, but don't distribute a second copy.
    [BepInPlugin("urakka.foundation.example", "Foundation Example", "0.5.0")]
    [BepInDependency("urakka.kingdominc.foundation", "0.5.0")]
    public sealed class ExamplePlugin : BaseUnityPlugin
    {
        ITechNode first, second, upgrade;
        IWorkspaceNodeType relay;

        void Awake()
        {
            // The interface keeps game UI details out of this plugin.
            ITechNodeRegistry nodes = FoundationApi.TechNodes;
            first = nodes.CreateTechNode(new TechNodeDefinition(
                "urakka.foundation.example.first", "Example Technology", new Vector2(-300, 180),
                "Research this technology to unlock its upgrade branch.",
                research: new TechNodeOptions(fixedCost: new CurrencyCollection(Currency.Gold, 100), iconName: "Icons_17")));
            upgrade = nodes.CreateTechNode(new TechNodeDefinition(
                "urakka.foundation.example.upgrade", "Example Upgrade", new Vector2(0, -160),
                "A native upgrade with a prerequisite, connector and saved unlock state.",
                research: new TechNodeOptions(nodeType: TechNodeType.Utility, rootId: first.Id, previousId: first.Id,
                    fixedCost: new CurrencyCollection(Currency.Gold, 25))));
            second = FoundationApi.CreateTechNode(new TechNodeDefinition(
                "urakka.foundation.example.second", "Another Technology", new Vector2(380, 180),
                "Created through Foundation, displayed and researched by the game.",
                research: new TechNodeOptions(fixedCost: new CurrencyCollection(Currency.Gold, 200))));
            first.Clicked += OnFirstClicked;
            relay = FoundationApi.RegisterWorkspaceNode(new WorkspaceNodeDefinition(
                Relay.TypeId, "Example Relay", () => new Relay(), first.Id,
                size: new Vector2Int(256, 128), description: "Passes items from its input to its output using native pipes.",
                fixedCost: new CurrencyCollection(Currency.Gold, 20)));
        }

        void OnFirstClicked(ITechNode node)
        {
            var current = node.Definition;
            node.Update(new TechNodeDefinition(node.Id, "Example Researched", current.Position,
                "Research complete. Click again to open the upgrade branch.", current.Icon, ((INativeTechNodeDefinition)current).Research));
            Logger.LogInfo("Example research unlocked: " + node.Id);
        }
        void OnDestroy()
        {
            relay?.Dispose(); upgrade?.Dispose(); first?.Dispose(); second?.Dispose();
        }
    }

    // Normal native model hooks: the framework handles registration, prefab lookup,
    // purchase menus and factory routing; the mod owns behavior and custom state.
    public sealed class Relay : WorkspaceNode
    {
        public const string TypeId = "urakka.foundation.example.relay";
        public int Transfers;
        public Relay() : base(TypeId)
        {
            Inputs.Add(new FlowInput(this, 0) { Position = -40, BufferSize = 1, Filter = item => true });
            Outputs.Add(new FlowOutput(this, 0) { Position = 40, BufferSize = 1 });
        }
        public override void Update(float delta)
        {
            if (!Inputs[0].HasBuffer || !Outputs[0].CanBuffer) return;
            Outputs[0].BufferedItems.Add(Inputs[0].ConsumeItem()); Transfers++;
        }
        protected override void DataToJson(JsonObject data) => data["ExampleTransfers"] = Transfers;
        protected override void DataFromJson(JsonObject data, FlowDiagram parent) => Transfers = data["ExampleTransfers"];
        protected override void CopySettingsTo(FlowElement copy) => ((Relay)copy).Transfers = Transfers;
    }
}
