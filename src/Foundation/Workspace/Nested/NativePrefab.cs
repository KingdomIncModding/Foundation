using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LightJson;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(FlowPrefabManager), nameof(FlowPrefabManager.Get))]
    internal static class NativePrefab
    {
        static bool Prefix(FlowPrefabManager __instance, string id, ref FlowPrefabSet __result)
        {
            var exists = WorkspaceNodeRegistry.Shared.TryResolve(id, out var registration);
            if (!exists && !MissingWorkspaceElement.IsMissing(id))
                return true;
            var cache = global::KingdomFoundation.WorkspaceIntegration.prefabs.GetOrCreateValue(__instance);
            // Null registration is used only for unknown saved nodes; use a distinct
            // fallback cache keyed by id to avoid mixing two absent plugins.
            if (!exists)
            {
                __result = global::KingdomFoundation.WorkspaceIntegration.MissingPrefab(__instance, id);
                return false;
            }

            if (!cache.TryGetValue(registration, out var prefab) || !prefab)
            {
                var manager = WorldFlowManager.Instance;
                if (!manager)
                    throw new InvalidOperationException("Workspace prefabs become available when a game scene is loaded.");
                var original = TechTreeTabs.Field<WorldFlowElement>(manager, "_defaultPrefab");
                var container = new GameObject("FoundationWorkspaceAssets:" + id);
                container.transform.SetParent(__instance.transform, false);
                container.SetActive(false);
                prefab = container.AddComponent<FlowPrefabSet>();
                prefab.Identifier = id;
                var world = global::UnityEngine.Object.Instantiate(original, container.transform, false);
                world.name = "WorkspaceNodePrefab:" + id;
                ((RectTransform)world.transform).sizeDelta = registration.Definition.Size;
                AccessTools.Field(typeof(FlowPrefabSet), "<Prefab>k__BackingField").SetValue(prefab, world);
                AccessTools.Field(typeof(FlowPrefabSet), "<DetailSprite>k__BackingField").SetValue(prefab, registration.Preview.Icon);
                cache[registration] = prefab;
            }

            __result = prefab;
            return false;
        }
    }
}
