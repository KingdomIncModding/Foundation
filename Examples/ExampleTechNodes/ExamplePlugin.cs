using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;
using Assets.Source.Player;
using Assets.Source.Flow;
using LightJson;

namespace FoundationExamples
{
    // Reference Foundation.dll in your project, but don't distribute a second copy.
    [BepInPlugin("urakka.foundation.example", "Foundation Example", "0.7.0")]
    [BepInDependency("urakka.kingdominc.foundation", "0.7.0")]
    public sealed class ExamplePlugin : BaseUnityPlugin
    {
        ITechNode first, second, upgrade;
        IWorkspaceNodeType relay;
        readonly System.Collections.Generic.List<IModBarRegistration> barItems = new System.Collections.Generic.List<IModBarRegistration>();

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
            // Each control is an IModBarElement. Registrations can be made before a HUD exists.
            const string mod = "urakka.foundation.example";
            var status = new ModBarText(mod, "status", "Example ready", width: 220, order: 0, alignment: ModBarAlignment.Left);
            var statusHandle = FoundationApi.ModBar.Register(status); barItems.Add(statusHandle);
            int clicks = 0;
            barItems.Add(FoundationApi.ModBar.Register(new ModBarButton(mod, "button", "Example button", () => {
                status.Text = "Clicks: " + ++clicks; statusHandle.Refresh();
            }, width: 220, order: 1, alignment: ModBarAlignment.Left)));
            barItems.Add(FoundationApi.ModBar.RegisterSeparator(mod, "separator", ModBarAlignment.Left, order: 2));
            barItems.Add(FoundationApi.ModBar.Register(new ModBarDropdown(mod, "period", new[] { "Second", "Minute", "Hour" }, selected: index => {
                status.Text = "Period: " + new[] { "Second", "Minute", "Hour" }[index]; statusHandle.Refresh();
            }, order: 3, alignment: ModBarAlignment.Left)));
            barItems.Add(FoundationApi.ModBar.Register(new ModBarToggle(mod, "enabled", "Example enabled", changed: value => {
                status.Text = value ? "Enabled" : "Disabled"; statusHandle.Refresh();
            }, width: 240, order: 0, alignment: ModBarAlignment.Center)));
            barItems.Add(FoundationApi.ModBar.Register(new CustomBarElement()));

            // Register a ModConfig section for in-game Options menu integration
            var config = FoundationApi.ModConfig.GetOrCreateSection(mod, "Foundation Example", "Example plugin settings demonstrating Foundation's ModConfig API.");
            config.AddText("Example Mod Configuration", isHeader: true);
            config.AddToggle("Example Feature", true, val => Logger.LogInfo("Example feature toggled: " + val));
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
            foreach (var item in barItems) item.Dispose();
            FoundationApi.ModConfig.RemoveSection("urakka.foundation.example");
        }
    }

    // A custom implementation uses the same contract as the built-in controls.
    // The context also exposes Root for icons, progress bars or other Unity UI children.
    public sealed class CustomBarElement : IModBarElement
    {
        public string Id => "custom";
        public string ModId => "urakka.foundation.example";
        public float Width => 200;
        public int Order => 0;
        public ModBarAlignment Alignment => ModBarAlignment.Right;
        public bool IsVisible => true;
        public IModBarView CreateView(IModBarContext context) => new CustomBarView(context);
        sealed class CustomBarView : IModBarView
        {
            readonly TMPro.TMP_Text label;
            internal CustomBarView(IModBarContext context) { label = context.CreateText("Custom view"); }
            public void Refresh() { label.text = "Custom view"; }
            public void Dispose() { /* Release external subscriptions here; Foundation destroys the slot. */ }
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
