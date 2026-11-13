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
    internal static class WorkspaceIntegration
    {
        [ThreadStatic]
        internal static int deserializeDepth;
        internal static readonly ConditionalWeakTable<FlowPrefabManager, Dictionary<WorkspaceNodeType, FlowPrefabSet>> prefabs = new ConditionalWeakTable<FlowPrefabManager, Dictionary<WorkspaceNodeType, FlowPrefabSet>>();
        internal static void Install(Harmony hooks)
        {
            foreach (var patch in new[]
            {
                typeof(global::KingdomFoundation.CreateModel),
                typeof(global::KingdomFoundation.BuildCatalog),
                typeof(global::KingdomFoundation.NativePrefab),
                typeof(global::KingdomFoundation.PreserveMissingJson),
                typeof(global::KingdomFoundation.DeserializeScope)
            }

            )
                hooks.CreateClassProcessor(patch).Patch();
            FoundationApi.WorkspaceNodes.Changed += RefreshMenu;
        }

        internal static void Remove() => FoundationApi.WorkspaceNodes.Changed -= RefreshMenu;
        static void RefreshMenu()
        {
            if (!GameUI.Instance || !GameUI.Instance.IsActive(GameUIType.BuyMenu))
                return;
            foreach (var menu in global::UnityEngine.Object.FindObjectsByType<ElementBuyMenu>(FindObjectsSortMode.None))
                if (menu.isActiveAndEnabled)
                    AccessTools.Method(typeof(ElementBuyMenu), "OnEnable").Invoke(menu, null);
        }

        static readonly ConditionalWeakTable<FlowPrefabManager, Dictionary<string, FlowPrefabSet>> missingPrefabs = new ConditionalWeakTable<FlowPrefabManager, Dictionary<string, FlowPrefabSet>>();
        internal static FlowPrefabSet MissingPrefab(FlowPrefabManager manager, string id)
        {
            var cache = missingPrefabs.GetOrCreateValue(manager);
            if (cache.TryGetValue(id, out var found) && found)
                return found;
            var worldManager = WorldFlowManager.Instance;
            if (!worldManager)
                throw new InvalidOperationException("Missing node previews require the game workspace scene.");
            var template = TechTreeTabs.Field<WorldFlowElement>(worldManager, "_defaultPrefab");
            var holder = new GameObject("FoundationMissingNode:" + id);
            holder.transform.SetParent(manager.transform, false);
            holder.SetActive(false);
            var set = holder.AddComponent<FlowPrefabSet>();
            set.Identifier = id;
            var element = global::UnityEngine.Object.Instantiate(template, holder.transform, false);
            element.name = "MissingNodePrefab:" + id;
            ((RectTransform)element.transform).sizeDelta = MissingWorkspaceElement.GetSize(id);
            AccessTools.Field(typeof(FlowPrefabSet), "<Prefab>k__BackingField").SetValue(set, element);
            AccessTools.Field(typeof(FlowPrefabSet), "<DetailSprite>k__BackingField").SetValue(set, SpriteLibrary.Get("Icons_104"));
            cache[id] = set;
            return set;
        }
    }
}
