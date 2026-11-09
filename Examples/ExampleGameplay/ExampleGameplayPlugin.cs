using System;
using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;

namespace FoundationExamples
{
    /// <summary>
    /// Independent consumer of Foundation's native gameplay + property UI services.
    /// This is documentation/sample source; it is not installed with the framework.
    /// </summary>
    [BepInPlugin(ModId, "Foundation Gameplay Example", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", "0.10.0")]
    public sealed class ExampleGameplayPlugin : BaseUnityPlugin
    {
        public const string ModId = "urakka.foundation.gameplay.example";
        FoundationModContext context;
        Color displayedColor = Color.green;
        bool reportTransfers = true;
        void Awake()
        {
            context = FoundationApi.ForMod(ModId);
            context.Listen<CurrencyChangedEvent>(evt =>
            {
                if (reportTransfers) Logger.LogInfo("Currency " + evt.Currency + " changed by " + evt.Delta);
            });
            context.Listen<MachineRecipeChangedEvent>(evt =>
                Logger.LogInfo("Recipe changed: " + evt.Machine.ElementType + " / " + evt.Current?.Identifier));
            context.Listen<KingdomAscendedEvent>(evt =>
                Logger.LogInfo("Kingdom ascension #" + evt.AscensionCount));

            // Bind a source with native-styled, automatically generated property controls.
            var source = context.Expose(new SignalSourceDefinition(ModId, ModId + ".options", "Gameplay Sample", new[]
            {
                new SignalPropertyDefinition("enabled", "Report Currency", typeof(bool),
                    () => reportTransfers, value => reportTransfers = (bool)value),
                new SignalPropertyDefinition("tint", "Indicator Tint", typeof(Color),
                    () => displayedColor, value => displayedColor = (Color)value)
            }));
            var config = context.Settings("Gameplay Example", "Shows game events and clickable rectangular color controls.");
            FoundationApi.PropertyControls.Build(config, source);
            config.AddButton("Show current kingdom", () =>
            {
                var player = FoundationApi.World.Player;
                Logger.LogInfo(player == null ? "No kingdom loaded" :
                    "Flows=" + FoundationApi.World.Flows.Count + ", Time=" + FoundationApi.World.GameTime);
            }, "Log Game State");
            Logger.LogInfo("Foundation gameplay API sample loaded; no game state modified.");
        }
        void OnDestroy() => context?.Dispose();
    }
}
