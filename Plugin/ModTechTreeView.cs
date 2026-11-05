using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.TechTree;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    // Refreshes the real game tree after API changes. It creates no UI of its own.
    internal sealed class ModTechTreeView : MonoBehaviour
    {
        TechTreeMenu menu;
        bool dirty;
        internal IReadOnlyDictionary<string, UITechNode> Views => menu.Nodes.ToDictionary(n => n.Tech.Identifier);
        internal void Initialize(TechTreeMenu owner) { menu = owner; Subscribe(); }
        void Subscribe() { FoundationApi.TechNodes.Changed -= MarkDirty; FoundationApi.TechNodes.Changed += MarkDirty; }
        void OnEnable() { if (menu) Subscribe(); }
        void OnDisable() => FoundationApi.TechNodes.Changed -= MarkDirty;
        void MarkDirty() => dirty = true;
        void LateUpdate() { if (dirty) Refresh(); }
        internal void Refresh()
        {
            dirty = false;
            var tabs = FoundationPlugin.Tabs(menu);
            bool categoryChanged = tabs && tabs.EnsureModCategory();
            if (!menu || !FoundationPlugin.ModsOpen) return;
            if (categoryChanged) { tabs.ShowSelectedMod(); return; }
            var camera = TechTreeTabs.Field<Camera>(menu, "_camera");
            var position = camera.transform.position;
            var root = menu.RootNode;
            if (categoryChanged || (root != null && (!NativeResearch.Active(root) || NativeResearch.ModOf(root)?.Id != tabs.SelectedModId))) root = null;
            menu.ShowTechTree(root, true);
            camera.transform.position = position;
        }
    }
}
