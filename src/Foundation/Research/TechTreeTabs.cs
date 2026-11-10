using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Provides the tech tree tabs API.</summary>
    public sealed partial class TechTreeTabs
    {
        internal string SelectedModId { get; private set; }
        internal ModResearchSidebar Sidebar;
        readonly Dictionary<string, ModState> modStates = new Dictionary<string, ModState>();


        void StoreModCategory()
        {
            if (SelectedModId != null) modStates[SelectedModId] = new ModState { Root = modsRoot, Camera = modsCamera };
        }
        void LoadModCategory()
        {
            modStates.TryGetValue(SelectedModId ?? "", out var saved);
            modsRoot = saved?.Root; modsCamera = saved?.Camera;
            if (modsRoot != null && (!NativeResearch.Active(modsRoot) || NativeResearch.ModOf(modsRoot)?.Id != SelectedModId)) modsRoot = null;
        }
        CameraState DefaultModCamera()
        {
            var state = Capture();
            var parent = Field<UnityEngine.RectTransform>(menu, "_nodeParent");
            var bounds = menu.Nodes.Select(n => ModResearchLayout.Bounds(n, parent)).ToArray();
            if (bounds.Length == 0) { state.Position = new UnityEngine.Vector3(0, 0, -10); return state; }
            var center = new UnityEngine.Vector2((bounds.Min(r => r.xMin) + bounds.Max(r => r.xMax)) / 2,
                (bounds.Min(r => r.yMin) + bounds.Max(r => r.yMax)) / 2);
            var corners = new UnityEngine.Vector3[4]; Sidebar.Panel.GetWorldCorners(corners);
            float reserved = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, corners[2]).x;
            state.Position = new UnityEngine.Vector3(center.x - reserved * state.Size / UnityEngine.Screen.height, center.y, -10);
            return state;
        }
        internal bool EnsureModCategory()
        {
            var mods = FoundationApi.TechMods;
            bool changed = !mods.Any(m => m.Id == SelectedModId);
            if (changed) { SelectedModId = mods.FirstOrDefault()?.Id; LoadModCategory(); }
            Sidebar?.Refresh();
            return changed;
        }
        internal void SelectMod(string id, bool sound = true)
        {
            if (!FoundationApi.TechMods.Any(m => m.Id == id)) return;
            if (SelectedModId == id) { Select(true, sound); return; }
            if (ModsSelected) { modsRoot = menu.RootNode; modsCamera = Capture(); StoreModCategory(); }
            SelectedModId = id; LoadModCategory();
            if (!ModsSelected) { Select(true, sound); return; }
            ShowSelectedMod();
            if (sound) UISounds.TurnPage();
        }
        internal void ShowSelectedMod()
        {
            list.gameObject.SetActive(false);
            Switching = true;
            try
            {
                menu.ShowTechTree(modsRoot, true);
                if (modsCamera == null) modsCamera = DefaultModCamera();
                Restore(modsCamera);
            }
            finally { Switching = false; }
            Sidebar.Refresh(); menu.UpdateTechListButton();
        }
        internal void SelectFor(TechNode node)
        {
            if (NativeResearch.Active(node)) SelectMod(NativeResearch.ModOf(node).Id, false);
            else Select(false, false);
        }
    }
}
