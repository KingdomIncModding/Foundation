using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using LightJson;
using UnityEngine;

namespace KingdomFoundation
{
    [BepInPlugin(Id, "Foundation", Version)]
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.foundation", Version = "0.11.0";
        internal static FoundationPlugin Instance;
        public static BepInEx.Configuration.ConfigEntry<string> NexusApiKeyConfig;
        internal Harmony hooks;
        internal static bool Diagnostic => Environment.GetCommandLineArgs().Contains("-foundation-smoke");

        /// <summary>Gets nexus api key.</summary>
        public static string GetNexusApiKey()
        {
            string key = NexusApiKeyConfig?.Value;
            if (!string.IsNullOrEmpty(key)) return key.Trim();
            key = Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.Process)
               ?? Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.User)
               ?? Environment.GetEnvironmentVariable("NEXUSMODS_API_KEY", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrEmpty(key)) return key.Trim();
            // Never distribute an API credential inside a public mod DLL.
            return null;
        }

        void Awake()
        {
            Instance = this;
            SetupDeveloperConsole();
            NexusApiKeyConfig = Config.Bind("Updates", "NexusApiKey", "", "Optional override for Nexus Mods API Key.");
            Assets.Source.Util.Translation.Init();
            hooks = new Harmony(Id);
            foreach (var type in new[] { typeof(OpenTree), typeof(CloseTree), typeof(ShowResearch), typeof(RefreshResearch), typeof(FocusResearch), typeof(RequirementNavigation), typeof(ListResearch), typeof(TreeUpdate), typeof(EmptyBounds), typeof(CustomIcon), typeof(FixedCosts), typeof(SaveNotifications), typeof(ReadResearch) })
                hooks.CreateClassProcessor(type).Patch();
            WorkspaceIntegration.Install(hooks);
            FlowIntegration.Install(hooks);
            SaveIntegration.Install(hooks);
            WorldEventIntegration.Install(hooks);
            GameEventIntegration.Install(hooks);
            UpgradeIntegration.Install(hooks);
            ItemIntegration.Install(hooks);
            GameplayIntegration.Install(hooks);
            FoundationNativeSignals.Install();
            // Machine is an in-process Foundation module, not a second plugin.
            MachineRuntime.Initialize(hooks, Logger);
            FoundationNativeMachinePanels.Install();
            hooks.CreateClassProcessor(typeof(CreateModBar)).Patch();
            hooks.CreateClassProcessor(typeof(EscapeModBar)).Patch();
            hooks.CreateClassProcessor(typeof(HideModBar)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIStartPatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIOnEnablePatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUIApplyPatch)).Patch();
            hooks.CreateClassProcessor(typeof(OptionsUICancelPatch)).Patch();
            if (Diagnostic)
            {
                // GameRoot can be isolated, but Unity's persistentDataPath remains
                // shared by all copies run under the same Windows user. Redirect
                // AND block native writes before the game scene starts.
                hooks.CreateClassProcessor(typeof(BlockExistingSaveInSmoke)).Patch();
                hooks.CreateClassProcessor(typeof(BlockAnyNativeSaveInSmoke)).Patch();
                IsolateSmokeSaves();
                InstallDiagnostics();
                Application.runInBackground = true;
                StartCoroutine(RuntimeCheck());
            }
            Logger.LogInfo("Foundation " + Version + ": native game services, content, quests, dungeons, wars, economy, IO, UI, events and SDK ready.");
        }
        // Unity's persistentDataPath is shared even if the game binary is copied
        // into a temporary sandbox. It is not safe to merely prevent reading saves:
        // GameManager.OnApplicationQuit always calls StoreAutosaveState, which would
        // otherwise overwrite the user's real autosave-exit.save.
        static void IsolateSmokeSaves()
        {
            string isolated = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification", "IsolatedSaves");
            Directory.CreateDirectory(isolated);
            SaveGame.SavesPath = isolated;
            AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, new DirectoryInfo(isolated));
            AccessTools.Field(typeof(SaveGame), "_saves").SetValue(null, null);
            UnityEngine.Debug.LogWarning("Foundation smoke mode: game saves redirected to " + isolated + " (native disk saves disabled).");
        }


        internal static TechTreeTabs Tabs(TechTreeMenu menu) => menu ? menu.GetComponent<TechTreeTabs>() : null;
        internal static bool ModsOpen => TechTreeMenu.Visible && Tabs(TechTreeMenu.Instance)?.ModsSelected == true;
        internal static IEnumerable<TechNode> VisibleResearch() => TechNode.Nodes.Where(n => ModsOpen
            ? NativeResearch.Active(n) && NativeResearch.ModOf(n)?.Id == Tabs(TechTreeMenu.Instance)?.SelectedModId
            : !NativeResearch.Owns(n));
        static IEnumerable<CodeInstruction> FilterCatalog(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(TechNode), nameof(TechNode.Nodes));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(FoundationPlugin), nameof(VisibleResearch)); }
                yield return instruction;
            }
        }
        void OnDestroy()
        {
            FoundationNativeMachinePanels.Uninstall();
            FoundationPanelHost.ResetAll();
            ((FoundationPanelRegistry)FoundationApi.Panels).Clear();
            MachineRuntime.Shutdown();
            WorkspaceIntegration.Remove();
            foreach (var bar in Resources.FindObjectsOfTypeAll<ModBarHost>()) Destroy(bar);
            foreach (var links in Resources.FindObjectsOfTypeAll<RequirementLinks>()) Destroy(links);
            foreach (var tabs in Resources.FindObjectsOfTypeAll<TechTreeTabs>()) Destroy(tabs);
            foreach (var menu in Resources.FindObjectsOfTypeAll<FoundationConfigMenu>()) Destroy(menu);
            hooks?.UnpatchSelf();
        }
















    }
}
