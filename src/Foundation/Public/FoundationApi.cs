using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace KingdomFoundation.Api
{
    /// <summary>Entry point shared by all mods that depend on Foundation.</summary>
    public static class FoundationApi
    {
        internal static readonly TechNodeRegistry Registry = new TechNodeRegistry(true);
        static readonly ManualLogSource Log = Logger.CreateLogSource("Foundation API");
        public static ITechNodeRegistry TechNodes => Registry;
        public static IReadOnlyList<ITechMod> TechMods => Registry.Mods;
        public static IWorkspaceNodeRegistry WorkspaceNodes => WorkspaceNodeRegistry.Shared;
        public static IModBarRegistry ModBar { get; } = new ModBarRegistry();
        private static readonly FoundationNativeUiFactory NativeUiFactoryService = new FoundationNativeUiFactory();
        /// <summary>Native Kingdom Inc UI creation bridge; existing mod-bar control contract unchanged.</summary>
        public static IFoundationNativeUiFactory NativeUiFactory => NativeUiFactoryService;
        public static IModConfigRegistry ModConfig { get; } = new ModConfigRegistry();
        /// <summary>Native-themed mod windows and type-specific machine panels (single unified DLL).</summary>
        public static IFoundationPanelRegistry Panels { get; } = new FoundationPanelRegistry();
        private static readonly FoundationAssets AssetService = new FoundationAssets();
        public static IFoundationAssets Assets => AssetService;
        /// <summary>Additional owner/dependency operations without modifying the legacy Assets contract.</summary>
        public static IFoundationAssetDirectory AssetDirectory => AssetService;
        private static readonly FoundationEventBus EventService = new FoundationEventBus();
        private static readonly SignalRegistry SignalService = new SignalRegistry();
        private static readonly FoundationSignalLinks SignalLinkService = new FoundationSignalLinks();
        private static readonly ModSaveRegistry SaveService = new ModSaveRegistry();
        private static readonly FoundationLifecycleService LifecycleService = new FoundationLifecycleService();
        private static readonly FoundationDiagnostics DiagnosticService = new FoundationDiagnostics();
        private static readonly FlowOperations FlowOperationsService = new FlowOperations();
        /// <summary>Typed, exception-isolated events with mod ownership and disposable subscriptions.</summary>
        public static IFoundationEventBus Events => EventService;
        /// <summary>Discover and register explicitly exposed values, including a source and its properties.</summary>
        public static ISignalRegistry Signals => SignalService;
        /// <summary>Additional type-aware lookup; original Signals interface remains unchanged.</summary>
        public static ISignalDirectory SignalDirectory => SignalService;
        /// <summary>Mod-created logical signal links, separate from native FlowPipe item transport.</summary>
        public static IFoundationSignalLinks SignalLinks => SignalLinkService;
        /// <summary>Per-kingdom versioned mod data, stored inside native saves.</summary>
        public static IModSaveRegistry ModSave => SaveService;
        /// <summary>Opt-in version migration over original FoundationModData storage.</summary>
        public static IModSaveMigrations SaveMigrations => SaveService;
        private static readonly FoundationResearchRecovery ResearchRecoveryService = new FoundationResearchRecovery();
        /// <summary>Native research compatibility diagnostics; never fabricates game-owned research state.</summary>
        public static IFoundationResearchRecovery ResearchRecovery => ResearchRecoveryService;
        /// <summary>Native game load/save/world lifecycle notifications.</summary>
        public static IFoundationLifecycleService Lifecycle => LifecycleService;
        public static IFoundationDiagnostics Diagnostics => DiagnosticService;
        /// <summary>Validated native item-pipe operations and observed throughput statistics.</summary>
        public static IFlowOperations IO => FlowOperationsService;
        /// <summary>Native FlowIO inspection/validation and logistics routing. Shares the same underlying native pipe operations.</summary>
        public static KingdomFoundation.Machines.MachineNativeIO NativeIO => KingdomFoundation.Machines.FoundationMachineApi.IO;
        internal static SignalRegistry SignalsInternal => SignalService;
        internal static ModSaveRegistry SaveDataInternal => SaveService;
        internal static FoundationLifecycleService LifecycleEvents => LifecycleService;
        internal static FlowOperations FlowOperationsInternal => FlowOperationsService;
        private static readonly GameViewService ViewService = new GameViewService();
        private static readonly GameAudioService AudioService = new GameAudioService();
        private static readonly GameWorldService WorldService = new GameWorldService();
        private static readonly WorkspaceEditorService WorkspaceEditor = new WorkspaceEditorService();
        private static readonly GameEconomyService EconomyService = new GameEconomyService();
        private static readonly GameProgressService ProgressService = new GameProgressService();
        private static readonly GameAdventureService AdventureService = new GameAdventureService();
        private static readonly GameItemsService ItemsService = new GameItemsService();
        private static readonly GameProductionService ProductionService = new GameProductionService();
        private static readonly GameMachinesService MachinesService = new GameMachinesService();
        private static readonly GamePresentationService PresentationService = new GamePresentationService();
        private static readonly GamePropertyControls PropertyControlsService = new GamePropertyControls();
        private static readonly GameLogisticsService LogisticsService = new GameLogisticsService();
        private static readonly GameSaveService GameSaves = new GameSaveService();
        private static readonly GameCombatService CombatService = new GameCombatService();
        private static readonly GameContentRegistry ContentService = new GameContentRegistry();
        private static readonly GameUpgradeRegistry UpgradeService = new GameUpgradeRegistry();
        private static readonly GameUiService UiService = new GameUiService();
        private static readonly GameFlagsService FlagsService = new GameFlagsService();
        private static readonly FoundationRuntimeService RuntimeService = new FoundationRuntimeService();
        private static readonly NativeGameBridge NativeBridge = new NativeGameBridge();
        private static readonly NativeSignalBinder SignalBinder = new NativeSignalBinder();
        public static IGameViewService View => ViewService;
        public static IGameAudioService Audio => AudioService;
        public static IGameWorldService World => WorldService;
        public static IWorkspaceEditorService Workspace => WorkspaceEditor;
        public static IGameEconomyService Economy => EconomyService;
        public static IGameProgressService Progression => ProgressService;
        public static IGameAdventureService Adventures => AdventureService;
        public static IGameItemsService Items => ItemsService;
        public static IGameProductionService Production => ProductionService;
        public static IGameMachinesService Machines => MachinesService;
        public static IGamePresentationService Presentation => PresentationService;
        public static IGamePropertyControls PropertyControls => PropertyControlsService;
        public static IGameLogisticsService Logistics => LogisticsService;
        public static IGameSaveService Saves => GameSaves;
        public static IGameCombatService Combat => CombatService;
        public static IGameContentRegistry Content => ContentService;
        /// <summary>Owner discovery in native content registrations; old Content API unchanged.</summary>
        public static IGameContentDirectory ContentDirectory => ContentService;
        internal static GameContentRegistry ContentInternal => ContentService;
        public static IGameUpgradeRegistry Upgrades => UpgradeService;
        public static IGameUiService UI => UiService;
        public static IGameFlagsService Flags => FlagsService;
        public static IFoundationRuntimeService Runtime => RuntimeService;
        public static INativeGameBridge Native => NativeBridge;
        public static INativeSignalBinder NativeSignals => SignalBinder;
        internal static FoundationRuntimeService RuntimeInternal => RuntimeService;

        private static readonly FlowInspectionService FlowService = new FlowInspectionService();
        /// <summary>Read-only native flow/IO inspection and pipe connection events.</summary>
        public static IFlowInspectionService Flow => FlowService;
        internal static FlowInspectionService FlowEvents => FlowService;
        /// <summary>Create an owner-scoped registration context; dispose when the plugin unloads.</summary>
        public static FoundationModContext ForMod(string modId) => new FoundationModContext(modId);
        public static ITechNode CreateTechNode(ITechNodeDefinition definition) => TechNodes.CreateTechNode(definition);
        public static IWorkspaceNodeType RegisterWorkspaceNode(IWorkspaceNodeDefinition definition) => WorkspaceNodes.RegisterWorkspaceNode(definition);
        public static void RegisterNexusMod(string guid, int nexusId) => NexusUpdateChecker.RegisterModMapping(guid, nexusId);
        internal static void Report(Exception error)
        {
            DiagnosticService.Write("Foundation", FoundationDiagnosticLevel.Error, error.ToString());
            Log.LogError("A Foundation subscriber failed: " + error);
        }
    }
}
