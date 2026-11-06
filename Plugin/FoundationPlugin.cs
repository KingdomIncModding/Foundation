using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomFoundation
{
    [BepInPlugin(Id, "Foundation", Version)]
    public sealed partial class FoundationPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.foundation", Version = "0.6.0";
        internal Harmony hooks;
        internal static bool Diagnostic => Environment.GetCommandLineArgs().Contains("-foundation-smoke");
        void Awake()
        {
            Assets.Source.Util.Translation.Init();
            hooks = new Harmony(Id);
            foreach (var type in new[] { typeof(OpenTree), typeof(CloseTree), typeof(ShowResearch), typeof(RefreshResearch), typeof(FocusResearch), typeof(RequirementNavigation), typeof(ListResearch), typeof(TreeUpdate), typeof(EmptyBounds), typeof(CustomIcon), typeof(FixedCosts), typeof(SaveNotifications), typeof(ReadResearch) })
                hooks.CreateClassProcessor(type).Patch();
            WorkspaceIntegration.Install(hooks);
            hooks.CreateClassProcessor(typeof(CreateModBar)).Patch();
            hooks.CreateClassProcessor(typeof(EscapeModBar)).Patch();
            hooks.CreateClassProcessor(typeof(HideModBar)).Patch();
            if (Diagnostic) { InstallDiagnostics(); Application.runInBackground = true; StartCoroutine(RuntimeCheck()); }
            Logger.LogInfo("Foundation " + Version + ": native research, workspace nodes and Mod Bar framework ready.");
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
            WorkspaceIntegration.Remove();
            foreach (var bar in Resources.FindObjectsOfTypeAll<ModBarHost>()) Destroy(bar);
            foreach (var links in Resources.FindObjectsOfTypeAll<RequirementLinks>()) Destroy(links);
            foreach (var tabs in Resources.FindObjectsOfTypeAll<TechTreeTabs>()) Destroy(tabs);
            hooks?.UnpatchSelf();
        }
        [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), "Start")]
        static class CreateModBar
        { static void Postfix(Assets.Behaviour.UI.GameUI __instance) => ModBarHost.Attach(__instance); }
        [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), nameof(Assets.Behaviour.UI.GameUI.ProcessEscape))]
        static class EscapeModBar
        { static bool Prefix(Assets.Behaviour.UI.GameUI __instance) => !__instance.GetComponentsInChildren<ModBarHost>(true).Any(host => host.ClosePopups()); }
        [HarmonyPatch(typeof(Assets.Behaviour.UI.GameUI), nameof(Assets.Behaviour.UI.GameUI.ToggleUI))]
        static class HideModBar
        {
            static void Postfix(Assets.Behaviour.UI.GameUI __instance)
            { if (!__instance.GetComponent<Canvas>().enabled) foreach (var host in __instance.GetComponentsInChildren<ModBarHost>(true)) host.ClosePopups(); }
        }
        [HarmonyPatch(typeof(TechTreeMenu), "OnEnable")]
        static class OpenTree
        {
            static void Postfix(TechTreeMenu __instance)
            { var tabs = Tabs(__instance) ?? __instance.gameObject.AddComponent<TechTreeTabs>(); tabs.Open(__instance); }
        }
        [HarmonyPatch(typeof(TechTreeMenu), "OnDisable")]
        static class CloseTree { static void Prefix(TechTreeMenu __instance) => Tabs(__instance)?.Close(); }
        [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.ShowTechTree))]
        static class ShowResearch
        {
            static void Prefix(TechTreeMenu __instance, ref TechNode root)
            {
                var tabs = Tabs(__instance);
                if (!tabs || tabs.Switching || root == null) return;
                if (NativeResearch.Owns(root) && !NativeResearch.Active(root)) root = null;
                else tabs.SelectFor(root);
            }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => FilterCatalog(instructions);
            static void Postfix(TechTreeMenu __instance) { if (ModsOpen) ModResearchLayout.Arrange(__instance); }
        }
        [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.UpdateTechTree))]
        static class RefreshResearch { static void Postfix(TechTreeMenu __instance) { if (ModsOpen) ModResearchLayout.Arrange(__instance); } }
        [HarmonyPatch(typeof(TechTreeMenu), nameof(TechTreeMenu.FocusNode))]
        static class FocusResearch
        {
            static bool Prefix(TechTreeMenu __instance, TechNode node)
            {
                Tabs(__instance)?.SelectFor(node);
                // Native focus assumes a zero-position node has a parent. Framework
                // roots may use the default origin, so handle that valid case here.
                if (node.Root != null || node.Position != Vector2.zero) return true;
                Assets.Behaviour.UI.UISounds.WindowOpen();
                __instance.ShowTechTree(null, true);
                TechTreeTabs.Field<Camera>(__instance, "_camera").transform.position = new Vector3(node.Position.x, node.Position.y, -10);
                return false;
            }
        }
        [HarmonyPatch(typeof(UIElementTechNode), nameof(UIElementTechNode.UpdateNodeStatus))]
        static class RequirementNavigation
        {
            static void Postfix(UIElementTechNode __instance, TMPro.TMP_Text ____requirementsText, RectTransform ____requirementsContainer)
            {
                if (!____requirementsText || !____requirementsContainer || !____requirementsContainer.gameObject.activeSelf) return;
                var links = ____requirementsContainer.GetComponent<RequirementLinks>() ?? ____requirementsContainer.gameObject.AddComponent<RequirementLinks>();
                links.Bind(__instance, ____requirementsText);
            }
        }
        [HarmonyPatch]
        static class ListResearch
        {
            static MethodBase TargetMethod() => AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(TechTreeListView), "_addNodes", Type.EmptyTypes));
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => FilterCatalog(instructions);
        }
        // Mods has its own camera state; keep the vanilla saved position on Base Game.
        [HarmonyPatch(typeof(TechTreeMenu), "Update")]
        static class TreeUpdate { static bool Prefix() => !ModsOpen; }
        [HarmonyPatch(typeof(WorldFlowCamera), nameof(WorldFlowCamera.RecalculateBounds))]
        static class EmptyBounds
        {
            static void Postfix(WorldFlowCamera __instance)
            {
                if (!ModsOpen || TechTreeMenu.Instance.Nodes.Any()) return;
                foreach (var name in new[] { "xMin", "yMin" }) AccessTools.Field(typeof(WorldFlowCamera), name).SetValue(__instance, -1000f);
                foreach (var name in new[] { "xMax", "yMax" }) AccessTools.Field(typeof(WorldFlowCamera), name).SetValue(__instance, 1000f);
            }
        }
        [HarmonyPatch(typeof(SpriteLibrary), nameof(SpriteLibrary.Get), new[] { typeof(string) })]
        static class CustomIcon
        {
            static bool Prefix(string name, ref Sprite __result)
            { if (!NativeResearch.TryIcon(name, out var sprite)) return true; __result = sprite; return false; }
        }
        [HarmonyPatch(typeof(TechNode), nameof(TechNode.ResetCost))]
        static class FixedCosts { static void Postfix(TechNode __instance) => NativeResearch.RestoreFixedCost(__instance); }
        // Native research uses a fixed-size array. Other consumers can retain player
        // snapshots created before a late registration, or register native tech themselves.
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.HasTech), new[] { typeof(TechNode) })]
        static class ReadResearch { static void Prefix(GamePlayer __instance) => NativeResearch.EnsureCapacity(__instance); }
        // Vanilla ignores missing researched IDs on load, but its NewTechCount dictionary
        // cannot accept missing keys. Keep Foundation's branch counters session-only.
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.ToJson))]
        static class SaveNotifications
        {
            static void Postfix(JsonValue __result)
            {
                var counts = __result["NewTechCount"].AsJsonObject;
                foreach (var node in TechNode.Nodes.Where(NativeResearch.Owns)) counts.Remove(node.Identifier);
            }
        }
    }
}
