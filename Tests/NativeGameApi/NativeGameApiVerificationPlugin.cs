using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Items.Data;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;

namespace Foundation.NativeGameApiVerification
{
    /// <summary>
    /// Development-only independent consumer of native-backed Foundation APIs.
    /// No native mutation occurs unless the user explicitly selects the separate
    /// disposable-save flag. This plugin is NOT a production Foundation module.
    /// </summary>
    [BepInPlugin("urakka.foundation.nativegameapi.verification", "Foundation Game API A3-A7 Verification", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NativeGameApiVerificationPlugin : BaseUnityPlugin
    {
        private sealed class Result { internal string Name, Status, Details; }
        private static readonly string[] Checks = {
            "Baseline.SingleFoundationAssembly", "Baseline.NativeAssembly", "Baseline.LiveKingdom",
            "A3.ItemCatalog", "A3.ItemUnlocked", "A3.ItemValue", "A3.ItemSerialization", "A3.ItemEnhancements",
            "A3.RecipeCatalog", "A3.RecipeInputsOutputs", "A3.RecipeCosts", "A3.RecipeAvailability",
            "A3.CurrencyParity", "A3.NativeCurrencyAffordability", "A3.ResourceCatalog", "A3.ResourcePrice",
            "A3.ForestWorkers", "A3.ForestQuotas", "A3.FarmGrid", "A3.FarmExpansionCosts",
            "A4.ResearchCatalog", "A4.ResearchRequirements", "A4.ResearchAvailability", "A4.ResearchCost",
            "A4.PrestigeCatalog", "A4.Quests", "A4.QuestRewards", "A4.DungeonRules", "A4.WarRules",
            "A4.NativeCombatStat", "A4.NativeCombatPower", "A4.NativeEquipmentRules",
            "A5.WorldIdentity", "A5.WorkspaceCatalog", "A5.ElementCatalog", "A5.NativeFlowIndices", "A5.NoOpMove",
            "A5.ReversibleCrossFlowMove",
            "A6.NativeUI", "A6.NativeInput", "A6.NativeCamera", "A6.NativeAudio",
            "A6.NativeLocalization", "A6.NativeSprites", "A6.NativeSaveSnapshot",
            "A7.NativeWorkspaceEvent", "A7.NativePopupEvent", "A7.NativeEquipmentEvent",
            "A7.NativeCurrencyEvent", "A7.NativeResearchEvent", "A7.NativeQuestEvent",
            "A7.NativeDungeonEvent", "A7.NativeWarEvent", "A7.NativeCombatVitalsEvent", "A7.EventIsolation"
        };
        private readonly Dictionary<string, Result> results = new Dictionary<string, Result>();
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private string reportPath;
        private bool wasLoaded, mutated;
        private int worldLoads;
        private float nextAudit;
        private bool allowMutation;
        private const string Marker = "[GAME-API VERIFY]";

        private void Mark(string id, string status, string details)
        {
            if (!results.TryGetValue(id, out var check)) return;
            if (check.Status == "FAIL" || check.Status == "PASS" && status != "FAIL") return;
            check.Status = status; check.Details = details;
            if (status == "FAIL") Logger.LogError(Marker + " FAIL " + id + ": " + details);
            else if (status == "PASS") Logger.LogInfo(Marker + " PASS " + id + ": " + details);
        }
        private void Verify(string id, Action work)
        {
            try { work(); }
            catch (Exception error) { Mark(id, "FAIL", error.ToString()); }
        }
        private void Compare(string id, bool predicate, string detail)
        {
            if (predicate) Mark(id, "PASS", detail);
            else Mark(id, "FAIL", detail);
        }
        private void Awake()
        {
            reportPath = Path.Combine(Paths.BepInExRootPath, "plugins", "Foundation.NativeGameApiVerification",
                "Reports", "gameapi-a3-a7.json");
            allowMutation = Environment.GetCommandLineArgs().Contains("-foundation-gameapi-mutate");
            foreach (var name in Checks) results[name] = new Result {
                Name = name, Status = "PENDING", Details = "Requires a live native kingdom or source-confirmed user action."
            };
            Verify("Baseline.SingleFoundationAssembly", () => Compare("Baseline.SingleFoundationAssembly",
                ReferenceEquals(FoundationApi.Items.GetType().Assembly, typeof(FoundationApi).Assembly) &&
                ReferenceEquals(FoundationApi.World.GetType().Assembly, typeof(FoundationApi).Assembly),
                "All A3-A7 service implementations ship in one Foundation.dll."));
            Verify("Baseline.NativeAssembly", () => Compare("Baseline.NativeAssembly",
                FoundationApi.Native.GameAssembly == typeof(GamePlayer).Assembly,
                "Native reflection bridge uses installed Assembly-CSharp."));

            // These are observations of native origins, not simulated callbacks.
            subscriptions.Add(FoundationApi.Events.Subscribe<NativeWorkspaceSelectedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Compare("A7.NativeWorkspaceEvent", ev.Player != null && ev.CurrentFlow == ev.Player.ActiveFlow && ev.CurrentIndex != ev.PreviousIndex,
                "Observed native WorldFlowManager.ShowFlow after switch."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<NativePopupVisibilityEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Compare("A7.NativePopupEvent", ev.Ui != null && ev.Popup && ev.IsVisible == ev.Popup.gameObject.activeSelf,
                "Observed native GameUI.TogglePopupMenu post-commit."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<NativeEquipmentChangedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Compare("A7.NativeEquipmentEvent", ev.Human != null && ev.Item != null &&
                ev.Equipped == ev.Human.Equipment.Contains(ev.Item),
                "Observed native HumanItemData.AddEquipment or RemoveEquipment."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<CurrencyChangedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Compare("A7.NativeCurrencyEvent", ev.Player.GetCurrency(ev.Currency) == ev.Current && ev.Previous != ev.Current,
                "Observed native GamePlayer.AddCurrency or SpendCurrency."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<ResearchUnlockedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Mark("A7.NativeResearchEvent", "PASS", "Observed native GamePlayer.AddTech."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<QuestCompletedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Mark("A7.NativeQuestEvent", "PASS", "Observed native quest completion."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<DungeonStageChangedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Mark("A7.NativeDungeonEvent", "PASS", "Observed native DungeonProgress.DungeonTick."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<WarStageChangedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            { Mark("A7.NativeWarEvent", "PASS", "Observed native WarProgress.UpdateWar."); Save(); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<NativeCombatantVitalsChangedEvent>("urakka.foundation.nativegameapi.verification", ev =>
            {
                Compare("A7.NativeCombatVitalsEvent", ev.Combatant != null &&
                    (ev.PreviousHealth01 != ev.CurrentHealth01 || ev.PreviousEnergy01 != ev.CurrentEnergy01),
                    "Observed native CombatantItemData vitals operation."); Save();
            }));
            // The existing Foundation event bus must isolate failing consumers.
            // This is a verifier-only synthetic event, not a fake game event.
            int received = 0;
            subscriptions.Add(FoundationApi.Events.Subscribe<int>("urakka.foundation.nativegameapi.verification", _ =>
            { throw new InvalidOperationException("Intentional event isolation verifier probe"); }));
            subscriptions.Add(FoundationApi.Events.Subscribe<int>("urakka.foundation.nativegameapi.verification", _ => received++));
            FoundationApi.Events.Publish(int.MinValue);
            Compare("A7.EventIsolation", received == 1, "Second observer runs after expected first observer exception.");
            Save();
        }
        private void Update()
        {
            bool loaded = GamePlayer.Current != null && GamePlayer.Current.Flows.Count > 0;
            if (loaded && !wasLoaded) { wasLoaded = true; worldLoads++; }
            else if (!loaded && wasLoaded) { wasLoaded = false; mutated = false; }
            if (!loaded || Time.unscaledTime < nextAudit) return;
            nextAudit = Time.unscaledTime + 2.5f;
            Mark("Baseline.LiveKingdom", "PASS", "Observed " + worldLoads + " live kingdom load(s).");
            Audit();
            Save();
        }
        private void Audit()
        {
            var player = GamePlayer.Current;
            if (player == null) return;
            var items = FoundationApi.Items;
            Verify("A3.ItemCatalog", () => Compare("A3.ItemCatalog", items.Items.Count == ItemType.All.Count &&
                items.Items.Count > 0, "Parity with native ItemType.All."));
            Verify("A3.ItemUnlocked", () => {
                var type = items.Items.FirstOrDefault();
                if (type != null) Compare("A3.ItemUnlocked", items.IsUnlocked(type) == type.IsUnlocked,
                    "Native ItemType.IsUnlocked getter parity.");
            });
            Verify("A3.ItemValue", () => {
                var type = items.Items.FirstOrDefault();
                if (type != null) Compare("A3.ItemValue", items.NativeBaseValue(type) == type.BaseValue,
                    "Native ItemType.BaseValue parity.");
            });
            Verify("A3.ItemSerialization", () => {
                var type = items.Items.FirstOrDefault();
                if (type == null) return;
                var sample = items.CreateItem(type.Identifier);
                var serialized = items.Serialize(sample);
                var replay = items.Deserialize(serialized);
                Compare("A3.ItemSerialization", replay.Type == sample.Type, "Native InventoryItem serializer roundtrip.");
            });
            Verify("A3.ItemEnhancements", () => {
                var catalog = items.Enhancements;
                Compare("A3.ItemEnhancements", catalog.Count != 0 && catalog.All(e =>
                    items.FindEnhancement(e.Identifier) == e), "Game-owned enhancement catalog discovered by native registry.");
            });
            var recipes = items.Recipes;
            Verify("A3.RecipeCatalog", () => Compare("A3.RecipeCatalog",
                recipes.Count == CraftingRecipe.All.Count && recipes.Count > 0,
                "Native CraftingRecipe.All parity."));
            var recipe = recipes.FirstOrDefault();
            if (recipe != null)
            {
                Verify("A3.RecipeInputsOutputs", () => Compare("A3.RecipeInputsOutputs",
                    items.RecipeReagents(recipe).Count == (recipe.Reagents?.Length ?? 0) &&
                    items.RecipeResults(recipe).Count == (recipe.Results?.Length ?? 0),
                    "Reagent/result counts match native recipe arrays."));
                Verify("A3.RecipeCosts", () => Compare("A3.RecipeCosts",
                    ReferenceEquals(items.RecipeCost(recipe), recipe.CraftingCost) &&
                    ReferenceEquals(items.RecipeReward(recipe), recipe.CraftingReward),
                    "Native CurrencyCollection references preserved without copying costs."));
                Verify("A3.RecipeAvailability", () => Compare("A3.RecipeAvailability",
                    items.RecipeAvailable(recipe) == recipe.IsAvailable, "Native required-tech rule parity."));
            }
            Verify("A3.CurrencyParity", () => Compare("A3.CurrencyParity",
                FoundationApi.Economy.Wallet().All(kv => kv.Value == player.GetCurrency(kv.Key)),
                "Native GamePlayer.GetCurrency snapshot parity."));
            Verify("A3.NativeCurrencyAffordability", () => Compare("A3.NativeCurrencyAffordability",
                FoundationApi.Economy.CanAfford(Currency.Gold, 0) == player.CanAfford(Currency.Gold, 0),
                "Native GamePlayer.CanAfford zero-cost readback."));
            Verify("A3.ResourceCatalog", () => Compare("A3.ResourceCatalog",
                FoundationApi.Economy.Resources().Count == player.GetAvailableResources().Count(),
                "Native GamePlayer.GetAvailableResources parity."));
            Verify("A3.ResourcePrice", () => {
                var resource = player.GetAvailableResources().FirstOrDefault().resource;
                if (resource == null) return;
                Compare("A3.ResourcePrice", FoundationApi.Economy.ResourceCostMultiplier(resource) ==
                    player.GetResourceCostMultiplier(resource), "Native resource-price multiplier parity.");
            });
            if (player.ForestSettings != null)
            {
                Verify("A3.ForestWorkers", () => Compare("A3.ForestWorkers",
                    FoundationApi.Production.TotalForestWorkers == player.ForestSettings.TotalWorkers,
                    "Native ForestSettings.TotalWorkers parity."));
                Verify("A3.ForestQuotas", () => Compare("A3.ForestQuotas",
                    FoundationApi.Production.ForestUtilization(ForestWorker.Unassigned) ==
                    player.ForestSettings.GetUtilization(ForestWorker.Unassigned),
                    "Native forest utilization parity."));
            }
            if (player.FarmSettings != null)
            {
                Verify("A3.FarmGrid", () => Compare("A3.FarmGrid",
                    FoundationApi.Production.Farm.Width == player.FarmSettings.Width &&
                    FoundationApi.Production.Farm.Height == player.FarmSettings.Height,
                    "Native FarmSettings dimensions parity."));
                Verify("A3.FarmExpansionCosts", () => Compare("A3.FarmExpansionCosts",
                    FoundationApi.Production.FarmExpansionCost(1).GetCurrency().SequenceEqual(
                        player.FarmSettings.GetPlotCost(1).GetCurrency()),
                    "Native FarmSettings.GetPlotCost(1) amount parity (not object identity)."));
            }

            var progress = FoundationApi.Progression;
            Verify("A4.ResearchCatalog", () => Compare("A4.ResearchCatalog",
                progress.Research.Count == TechNode.Nodes.Count(), "Native TechNode.Nodes parity."));
            var tech = progress.Research.FirstOrDefault();
            if (tech != null)
            {
                Verify("A4.ResearchRequirements", () => Compare("A4.ResearchRequirements",
                    progress.Requirements(tech).SequenceEqual(tech.AllRequirements), "Native AllRequirements parity."));
                Verify("A4.ResearchAvailability", () => Compare("A4.ResearchAvailability",
                    progress.ResearchAvailable(tech) == tech.IsAvailable && progress.ResearchLocked(tech) == tech.IsLocked,
                    "Native research availability and locking parity."));
                Verify("A4.ResearchCost", () => Compare("A4.ResearchCost",
                    ReferenceEquals(progress.ResearchCost(tech), tech.GetCost()),
                    "Native research cost object, including prestige discounts."));
            }
            Verify("A4.PrestigeCatalog", () => Compare("A4.PrestigeCatalog",
                progress.Prestige.Count == PrestigeNode.All.Count(), "Native prestige registry parity."));
            var adventure = FoundationApi.Adventures;
            var quest = adventure.Quests.FirstOrDefault();
            if (quest != null)
            {
                Verify("A4.Quests", () => Compare("A4.Quests",
                    adventure.QuestDuration(quest) == quest.TickDuration && adventure.QuestUnlocked(quest) == quest.IsUnlocked,
                    "Native quest duration and unlock rules."));
                Verify("A4.QuestRewards", () => Compare("A4.QuestRewards",
                    adventure.QuestRewards(quest).SequenceEqual(quest.GetRewards(quest.GetMetadata().CompletedCount) ?? Array.Empty<QuestReward>()),
                    "Native quest first/repeat reward calculation."));
            }
            var dungeon = adventure.Dungeons.FirstOrDefault();
            if (dungeon != null)
                Verify("A4.DungeonRules", () => Compare("A4.DungeonRules",
                    adventure.IsDungeonAvailable(dungeon) == dungeon.IsAvailableFor(player) &&
                    adventure.DungeonStages(dungeon).SequenceEqual(dungeon.Stages),
                    "Native DungeonType.IsAvailableFor and Stages parity."));
            var war = adventure.FindWar("forest");
            if (war != null)
                Verify("A4.WarRules", () => Compare("A4.WarRules",
                    adventure.HasWarStage(war, 0) == player.HasWarStage(war, 0),
                    "Native GamePlayer.HasWarStage parity."));
            // Read native item preview only. Do not create or recruit a character.
            Verify("A4.NativeCombatStat", () => {
                var combatant = items.Items.Where(i => i is Assets.Source.Items.Type.CombatantItemType)
                    .Select(i => i.PreviewItem?.Data as CombatantItemData).FirstOrDefault(d => d != null);
                if (combatant == null) return;
                Compare("A4.NativeCombatStat", FoundationApi.Combat.Stat(combatant, EquipStat.Health) ==
                    combatant.GetStat(EquipStat.Health), "Native combatant health stat parity.");
                Compare("A4.NativeCombatPower", FoundationApi.Combat.CombatPower(combatant) ==
                    combatant.GetCombatPower(), "Native CombatantItemData.GetCombatPower parity.");
                var human = items.Items.Where(i => i is Assets.Source.Items.Type.HumanItemType)
                    .Select(i => i.PreviewItem?.Data as HumanItemData).FirstOrDefault(d => d != null);
                if (human != null)
                    Compare("A4.NativeEquipmentRules", FoundationApi.Combat.Equipment(human).Count == human.Equipment.Count,
                        "Native human equipment count parity.");
            });

            var world = FoundationApi.World;
            Verify("A5.WorldIdentity", () => Compare("A5.WorldIdentity", ReferenceEquals(world.Player, player) &&
                ReferenceEquals(world.ActiveFlow, player.ActiveFlow), "Native GamePlayer.Current and ActiveFlow parity."));
            Verify("A5.WorkspaceCatalog", () => Compare("A5.WorkspaceCatalog", world.Flows.Count == player.Flows.Count,
                "Native GamePlayer.Flows parity."));
            Verify("A5.ElementCatalog", () => Compare("A5.ElementCatalog",
                world.NativeMachineCount(player.ActiveFlow) == player.ActiveFlow.Elements.Count &&
                world.NativePipeCount(player.ActiveFlow) == player.ActiveFlow.Pipes.Count &&
                world.NativeDecorationCount(player.ActiveFlow) == player.ActiveFlow.Decorations.Count,
                "Native FlowDiagram live machine/pipe/deco counts."));
            Verify("A5.NativeFlowIndices", () => Compare("A5.NativeFlowIndices",
                world.ActiveFlowIndex == player.CurrentFlow &&
                ReferenceEquals(world.GetFlow(player.CurrentFlow), player.ActiveFlow),
                "Native GamePlayer.CurrentFlow parity."));
            if (player.ActiveFlow.Elements.Count != 0)
                Verify("A5.NoOpMove", () => {
                    var element = player.ActiveFlow.Elements[0];
                    var flowIndex = player.CurrentFlow;
                    var count = player.ActiveFlow.Elements.Count;
                    var existingParent = element.ParentFlow;
                    FoundationApi.Workspace.MoveToFlow(element, flowIndex);
                    Compare("A5.NoOpMove", player.CurrentFlow == flowIndex &&
                        player.ActiveFlow.Elements.Count == count &&
                        player.ActiveFlow.Elements.Contains(element) &&
                        ReferenceEquals(existingParent, element.ParentFlow),
                        "A native no-op move preserved workspace, element count, and element ownership.");
                });
            if (allowMutation && !mutated)
            {
                TryReversibleMove();
                // Two workspaces and an unconnected machine may be created
                // AFTER the verifier first loads. Keep this test PENDING and
                // retry discovery instead of permanently skipping it.
                mutated = results["A5.ReversibleCrossFlowMove"].Status != "PENDING";
            }

            Verify("A6.NativeUI", () => {
                if (!FoundationApi.UI.IsReady || !GameUI.Instance) return;
                Compare("A6.NativeUI", FoundationApi.UI.NativeUi == GameUI.Instance,
                    "Existing GameUI.Instance returned; no replacement UI created.");
            });
            Verify("A6.NativeInput", () => {
                if (!FoundationApi.View.NativeInput) return;
                Compare("A6.NativeInput", FoundationApi.View.NativeActionAsset ==
                    Assets.Behaviour.Input.PlayerInput.Asset,
                    "Native input action asset parity.");
            });
            Verify("A6.NativeCamera", () => {
                if (!FoundationApi.View.NativeCamera) return;
                Compare("A6.NativeCamera", FoundationApi.View.NativeCamera == WorldFlowCamera.Instance &&
                    FoundationApi.View.CameraZoom == WorldFlowCamera.Instance.ZoomLevel,
                    "Native WorldFlowCamera and zoom parity.");
            });
            Verify("A6.NativeAudio", () => {
                if (!FoundationApi.Audio.IsReady) return;
                Compare("A6.NativeAudio", FoundationApi.Audio.IsPlaying ==
                    Assets.Behaviour.Util.MusicManager.IsPlaying, "Native MusicManager parity.");
            });
            Verify("A6.NativeLocalization", () => Compare("A6.NativeLocalization",
                FoundationApi.Presentation.NativeLocales.Contains(FoundationApi.Presentation.CurrentLocale),
                "Game-owned Translation.All/current locale parity."));
            Verify("A6.NativeSprites", () => Compare("A6.NativeSprites",
                FoundationApi.Presentation.IconNames.Count > 0,
                "Game-owned SpriteLibrary catalog exposed."));
            // SaveCurrentState can trigger game save observers, so only sample
            // once per runtime load rather than every 2.5-second audit tick.
            if (results["A6.NativeSaveSnapshot"].Status == "PENDING")
                Verify("A6.NativeSaveSnapshot", () => Compare("A6.NativeSaveSnapshot",
                    FoundationApi.Saves.CreateSnapshot().ContainsKey("Player"),
                    "Native SaveGame.SaveCurrentState JSON contains the Player section."));
        }
        private void TryReversibleMove()
        {
            Verify("A5.ReversibleCrossFlowMove", () =>
            {
                var player = GamePlayer.Current;
                if (player.Flows.Count < 2) return;
                var from = player.CurrentFlow;
                var to = from == 0 ? 1 : 0;
                var candidate = player.ActiveFlow.Elements.FirstOrDefault(element => element.World &&
                    element.IO.All(port => port.ConnectedPipe == null));
                if (candidate == null) return;
                // Explicit opt-in ONLY on disposable saves. The original graph is
                // restored through the same native manager in a finally block.
                try
                {
                    FoundationApi.Workspace.MoveToFlow(candidate, to);
                    if (!player.Flows[to].Elements.Contains(candidate))
                        throw new InvalidOperationException("Native MoveToFlow did not update target graph.");
                }
                finally
                {
                    if (player.Flows[to].Elements.Contains(candidate)) FoundationApi.Workspace.MoveToFlow(candidate, from);
                }
                Compare("A5.ReversibleCrossFlowMove", player.Flows[from].Elements.Contains(candidate) &&
                    !player.Flows[to].Elements.Contains(candidate), "Native WorldFlowManager.MoveToFlow moved and restored detached machine.");
            });
        }
        private static string Escape(string s)
        {
            if (s == null) return "null";
            var value = new StringBuilder("\"");
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': value.Append("\\\\"); break;
                    case '"': value.Append("\\\""); break;
                    case '\n': value.Append("\\n"); break;
                    case '\r': value.Append("\\r"); break;
                    case '\t': value.Append("\\t"); break;
                    default:
                        if (c < 32 || char.IsSurrogate(c)) value.Append("\\u").Append(((int)c).ToString("x4"));
                        else value.Append(c);
                        break;
                }
            }
            return value.Append('"').ToString();
        }
        private static string Digest(string path)
        {
            if (!File.Exists(path)) return null;
            using (var input = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
        }
        private void Save()
        {
            try
            {
                var dll = Path.Combine(Paths.BepInExRootPath, "plugins", "Foundation", "Foundation.dll");
                var content = new StringBuilder("{\n  \"generatedUtc\": ").Append(Escape(DateTime.UtcNow.ToString("o")));
                content.Append(",\n  \"foundationVersion\": ")
                    .Append(Escape(typeof(FoundationApi).Assembly.GetName().Version.ToString()));
                content.Append(",\n  \"foundationSha256\": ").Append(Escape(Digest(dll)));
                content.Append(",\n  \"gameAssemblySha256\": ").Append(Escape(Digest(Path.Combine(
                    Paths.BepInExRootPath, "..", "KingdomInc_Data", "Managed", "Assembly-CSharp.dll"))));
                content.Append(",\n  \"worldLoads\": ").Append(worldLoads);
                content.Append(",\n  \"mutationEnabled\": ").Append(allowMutation ? "true" : "false");
                content.Append(",\n  \"checks\": [");
                int n = 0;
                foreach (var check in results.Values)
                    content.Append(n++ == 0 ? "\n" : ",\n").Append("    {\"id\": ").Append(Escape(check.Name))
                        .Append(", \"result\": ").Append(Escape(check.Status))
                        .Append(", \"details\": ").Append(Escape(check.Details)).Append('}');
                content.Append("\n  ]\n}\n");
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, content.ToString(), new UTF8Encoding(false));
            }
            catch (Exception error) { Logger.LogWarning(Marker + " Cannot save report: " + error.Message); }
        }
        private void OnDestroy()
        {
            foreach (var subscription in subscriptions) subscription.Dispose();
            subscriptions.Clear();
            Save();
        }
    }
}
