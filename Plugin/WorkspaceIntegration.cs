using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    internal static class WorkspaceIntegration
    {
        static readonly ConditionalWeakTable<FlowPrefabManager, Dictionary<WorkspaceNodeType, FlowPrefabSet>> prefabs = new ConditionalWeakTable<FlowPrefabManager, Dictionary<WorkspaceNodeType, FlowPrefabSet>>();
        internal static void Install(Harmony hooks)
        {
            foreach (var patch in new[] { typeof(CreateModel), typeof(BuildCatalog), typeof(NativePrefab) }) hooks.CreateClassProcessor(patch).Patch();
            FoundationApi.WorkspaceNodes.Changed += RefreshMenu;
        }
        internal static void Remove() => FoundationApi.WorkspaceNodes.Changed -= RefreshMenu;
        static void RefreshMenu()
        {
            if (!GameUI.Instance || !GameUI.Instance.IsActive(GameUIType.BuyMenu)) return;
            foreach (var menu in UnityEngine.Object.FindObjectsByType<ElementBuyMenu>(FindObjectsSortMode.None))
                if (menu.isActiveAndEnabled) AccessTools.Method(typeof(ElementBuyMenu), "OnEnable").Invoke(menu, null);
        }
        [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.Create))]
        static class CreateModel
        {
            [HarmonyPriority(Priority.First)]
            static bool Prefix(string name, ref FlowElement __result)
            {
                if (WorkspaceNodeRegistry.Shared.TryResolve(name, out var registration))
                {
                    // Inactive registrations remain loadable for placed nodes during this session.
                    __result = registration.CreateModel(); return false;
                }
                // Type.GetType in the native method can resolve against Harmony's
                // generated assembly. Resolve built-in types in the game assembly.
                var native = typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements." + name);
                if (native == null) return true;
                __result = (FlowElement)Activator.CreateInstance(native); return false;
            }
        }
        [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.GetAllTypes))]
        static class BuildCatalog
        {
            static void Postfix(ref IEnumerable<FlowElement> __result) => __result = WorkspaceNodeRegistry.Shared.Include(__result);
        }
        [HarmonyPatch(typeof(FlowPrefabManager), nameof(FlowPrefabManager.Get))]
        static class NativePrefab
        {
            static bool Prefix(FlowPrefabManager __instance, string id, ref FlowPrefabSet __result)
            {
                if (!WorkspaceNodeRegistry.Shared.TryResolve(id, out var registration)) return true;
                var cache = prefabs.GetOrCreateValue(__instance);
                if (!cache.TryGetValue(registration, out var prefab) || !prefab)
                {
                    var manager = WorldFlowManager.Instance;
                    if (!manager) throw new InvalidOperationException("Workspace prefabs become available when a game scene is loaded.");
                    var original = TechTreeTabs.Field<WorldFlowElement>(manager, "_defaultPrefab");
                    var container = new GameObject("FoundationWorkspaceAssets:" + id);
                    container.transform.SetParent(__instance.transform, false); container.SetActive(false);
                    prefab = container.AddComponent<FlowPrefabSet>(); prefab.Identifier = id;
                    var world = UnityEngine.Object.Instantiate(original, container.transform, false);
                    world.name = "WorkspaceNodePrefab:" + id;
                    ((RectTransform)world.transform).sizeDelta = registration.Definition.Size;
                    AccessTools.Field(typeof(FlowPrefabSet), "<Prefab>k__BackingField").SetValue(prefab, world);
                    AccessTools.Field(typeof(FlowPrefabSet), "<DetailSprite>k__BackingField").SetValue(prefab, registration.Preview.Icon);
                    cache[registration] = prefab;
                }
                __result = prefab; return false;
            }
        }
    }
}
