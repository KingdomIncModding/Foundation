using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Game-owned, read-only signal sources available to every mod's two-stage
    /// source/property selector. Values are taken directly from live game objects;
    /// no replacement simulation or custom state is maintained.
    /// </summary>
    internal static class FoundationNativeSignals
    {
        const string Owner = "urakka.kingdominc.foundation";

        internal static void Install()
        {
            Register("foundation:player", "Kingdom — Player", new[]
            {
                Boolean("loaded", "Kingdom Loaded", () => GamePlayer.Current != null),
                Integer("time", "Game Time", () => GamePlayer.Current?.GameTime ?? 0),
                Integer("flowIndex", "Current Workspace", () => GamePlayer.Current?.CurrentFlow ?? -1),
                Integer("flowCount", "Workspace Count", () => GamePlayer.Current?.Flows.Count ?? 0),
                Integer("ascensions", "Ascensions", () => GamePlayer.Current?.AscensionCount ?? 0),
                Boolean("nightmareActive", "Nightmare Active", () => GamePlayer.Current?.NightmareActive ?? false),
                Number("nightmareProgress", "Nightmare Progress", () => GamePlayer.Current?.NightmareProgress ?? 0f)
            });
            Register("foundation:workspace", "Kingdom — Active Workspace", new[]
            {
                Boolean("available", "Workspace Available", () => CurrentFlow() != null),
                Text("label", "Label", () => CurrentFlow()?.Label ?? string.Empty),
                Text("icon", "Icon", () => CurrentFlow()?.IconName ?? string.Empty),
                Integer("elements", "Machine Count", () => CurrentFlow()?.Elements.Count ?? 0),
                Integer("pipes", "Pipe Count", () => CurrentFlow()?.Pipes.Count ?? 0),
                Integer("decorations", "Decoration Count", () => CurrentFlow()?.Decorations.Count ?? 0)
            });
            Register("foundation:camera", "Kingdom — Workspace Camera", new[]
            {
                Boolean("available", "Camera Available", () => WorldFlowCamera.Instance),
                Integer("zoom", "Zoom Level", () => WorldFlowCamera.Instance ? WorldFlowCamera.Instance.ZoomLevel : -1),
                Integer("maxZoom", "Maximum Zoom", () => WorldFlowCamera.Instance ? WorldFlowCamera.Instance.MaxZoom : 0),
                Number("zoomScale", "Zoom Scale", () => WorldFlowCamera.Instance ? WorldFlowCamera.Instance.ZoomScale : 0f)
            });
        }

        static FlowDiagram CurrentFlow()
        {
            var player = GamePlayer.Current;
            if (player == null || player.CurrentFlow < 0 || player.CurrentFlow >= player.Flows.Count) return null;
            return player.Flows[player.CurrentFlow];
        }
        static SignalPropertyDefinition Boolean(string id, string label, Func<bool> read)
            => new SignalPropertyDefinition(id, label, typeof(bool), () => read());
        static SignalPropertyDefinition Integer(string id, string label, Func<int> read)
            => new SignalPropertyDefinition(id, label, typeof(int), () => read());
        static SignalPropertyDefinition Number(string id, string label, Func<float> read)
            => new SignalPropertyDefinition(id, label, typeof(float), () => read());
        static SignalPropertyDefinition Text(string id, string label, Func<string> read)
            => new SignalPropertyDefinition(id, label, typeof(string), () => read());
        static void Register(string id, string label, SignalPropertyDefinition[] properties)
        {
            // The player may return to the main menu and create multiple kingdoms
            // without a domain reload. Sources are registered once, getters stay live.
            if (!FoundationApi.Signals.TryGet(id, out _))
                FoundationApi.Signals.Register(new SignalSourceDefinition(Owner, id, label, properties));
        }
    }
}
