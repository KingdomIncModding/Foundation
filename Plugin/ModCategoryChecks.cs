using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        IEnumerator VerifyModCategories(TechTreeMenu menu, TechTreeTabs tabs, Camera camera, string directory)
        {
            const string alpha = "foundation.layout.alpha", beta = "foundation.layout.beta";
            var example = FoundationApi.TechMods.Single(m => m.Id == "urakka.foundation.example");
            Check(example.Name == "Foundation Example" && example.Nodes.Count == 3, "Existing separately compiled consumers automatically receive their BepInEx mod category");
            Check(tabs.Sidebar.Panel.gameObject.activeSelf && tabs.SelectedModId == example.Id, "Mods opens with its native-style left mod list and a selected category");
            Check(menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == example.Id), "The selected category excludes other mods' root technologies");
            Check(FoundationApi.TechMods.Sum(m => m.Nodes.Count) == FoundationApi.TechNodes.Nodes.Count, "Every active registration belongs to exactly one mod category");
            var registrations = new List<ITechNode>();
            Func<string, string, string, string, TechNodeOptions, ITechNode> add = (id, name, mod, title, rules) =>
            {
                var result = FoundationApi.CreateTechNode(new TechNodeDefinition(id, name, mod, title, research: rules));
                registrations.Add(result); return result;
            };
            try
            {
                var a = add("foundation.layout.a", "Alpha Root", alpha, "Layout Alpha", null);
                var b = add("foundation.layout.b", "Beta Root", beta, "Layout Beta", null);
                var sibling = add("foundation.layout.sibling", "Same Requested Position", alpha, "Layout Alpha", null);
                var dependent = add("foundation.layout.dependent", "Cross-Mod Dependency", alpha, "Layout Alpha",
                    new TechNodeOptions(requirements: new[] { b.Id, "requisition_office" }));
                var child = add("foundation.layout.child", "Child At Parent Position", alpha, "Layout Alpha",
                    new TechNodeOptions(nodeType: TechNodeType.Utility, rootId: a.Id, previousId: a.Id));
                add("foundation.layout.resource", "Resource At Parent Position", alpha, "Layout Alpha",
                    new TechNodeOptions(nodeType: TechNodeType.Resource, rootId: a.Id));
                var categories = FoundationApi.TechMods;
                Check(categories.Count(m => m.Id == alpha) == 1 && categories.Single(m => m.Id == alpha).Nodes.Count == 5, "Multiple technologies register under one explicit mod category");
                Throws<ArgumentException>(() => a.Update(new TechNodeDefinition(a.Id, "Wrong owner", beta, "Layout Beta")), "Node updates cannot silently move ownership to another mod");
                Throws<ArgumentException>(() => FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.layout.invalid", "Wrong branch", alpha, "Layout Alpha",
                    research: new TechNodeOptions(rootId: b.Id))), "Cross-mod branch parents are rejected; cross-mod prerequisites remain supported");
                var aModel = ((INativeTechNode)a).NativeNode; var bModel = ((INativeTechNode)b).NativeNode;
                menu.FocusNode(aModel);
                yield return null;
                Check(tabs.SelectedModId == alpha && tabs.Sidebar.Buttons.ContainsKey(alpha) && tabs.Sidebar.Buttons.ContainsKey(beta), "Late registration adds both categories to the live mod list");
                Check(menu.Nodes.Count() == 3 && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == alpha), "Selecting Alpha renders only its three root nodes");
                Check(a.Definition.Position == sibling.Definition.Position && aModel.Position != ((INativeTechNode)sibling).NativeNode.Position, "Duplicate requested coordinates are resolved automatically without changing definitions");
                CheckNoOverlap(menu, "Category root frames, costs and multi-line requirement panels do not overlap");
                var positions = menu.Nodes.ToDictionary(n => n.Tech.Identifier, n => n.Tech.Position);
                menu.UpdateTechTree(); menu.UpdateTechTree();
                Check(menu.Nodes.All(n => positions[n.Tech.Identifier] == n.Tech.Position), "Repeated status refreshes preserve resolved positions without layout drift");
                camera.transform.position += new Vector3(350, 0, 0);
                Capture(camera, Path.Combine(directory, "foundation-mod-list.png"));
                Click(tabs.Sidebar.Buttons[beta]);
                yield return null;
                Check(tabs.SelectedModId == beta && menu.Nodes.Count() == 1 && menu.Nodes.Single().Tech == bModel, "A real sidebar button switches to Beta's independent research map");
                Check(aModel.Position == bModel.Position, "Different category pages can reuse the same coordinates without sharing a visible space");
                menu.FocusNode(((INativeTechNode)child).NativeNode);
                yield return null;
                Check(tabs.SelectedModId == alpha && menu.RootNode == aModel && menu.Nodes.Any(n => n.Tech.Identifier == child.Id), "Native focus automatically switches mod category and opens the target branch");
                CheckNoOverlap(menu, "A branch root and child at identical requested coordinates are separated");
                Check(menu.Nodes.Any(n => n.Tech.Identifier == "foundation.layout.resource"), "Native generic resource nodes participate in the same branch layout");
                var childModel = ((INativeTechNode)child).NativeNode;
                var parent = TechTreeTabs.Field<RectTransform>(menu, "_nodeParent");
                Check(parent.GetComponentsInChildren<LineRenderer>().Any(line => line.positionCount == 2 &&
                    (Vector2)line.GetPosition(0) == childModel.Position && (Vector2)line.GetPosition(1) == aModel.Position), "Native connectors follow the resolved node positions");
                CheckFocused(camera, childModel, "Focus centers the actual resolved branch node");
                WorldFlowCamera.Instance.Move(72, 48);
                var savedPosition = camera.transform.position; var savedSize = camera.orthographicSize;
                Click(tabs.Sidebar.Buttons[beta]); Click(tabs.Sidebar.Buttons[alpha]);
                Check(menu.RootNode == aModel && camera.transform.position == savedPosition && camera.orthographicSize == savedSize, "Every category retains its branch, camera position and zoom");
                menu.FocusNode(((INativeTechNode)dependent).NativeNode);
                yield return null;
                RequirementClick(Node(menu, dependent.Id), camera, 0);
                Check(tabs.SelectedModId == beta && menu.Nodes.Single().Tech == bModel && !bModel.IsPurchased, "A requirement link selects its owning mod category without purchasing research");
                menu.FocusNode(aModel);
                sibling.Update(new TechNodeDefinition(sibling.Id, "Moved Onto Root", (Vector2)aModel.Position));
                yield return null;
                CheckNoOverlap(menu, "Live definition updates cannot place a technology over another visible technology");
                for (int i = 0; i < 14; i++) add("foundation.layout.scroll." + i, "Scroll Root", "foundation.layout.scrollmod." + i, "Scroll Mod " + i.ToString("00"), null);
                Check(FoundationApi.TechMods.Count == categories.Count + 14, "Category enumeration snapshots remain stable after new mod registrations");
                yield return null;
                Canvas.ForceUpdateCanvases();
                var scroll = tabs.Sidebar.Scroll;
                Check(scroll.content.rect.height > scroll.viewport.rect.height, "The mod list scrolls when installed categories exceed its height");
                var eventData = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -12) };
                ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.scrollHandler);
                Check(scroll.content.anchoredPosition.y > 0, "Native ScrollRect wheel handling reveals off-screen mod categories");
                var blocker = tabs.Sidebar.Panel.GetComponent<Assets.Behaviour.UI.Shared.UIBlocksZoom>();
                blocker.OnPointerEnter(eventData);
                var zoomBeforeScroll = camera.orthographicSize;
                Wheel(UnityEngine.InputSystem.Mouse.current, -120);
                Check(camera.orthographicSize == zoomBeforeScroll, "Mouse wheel over the mod list is blocked from zooming the research map");
                blocker.OnPointerExit(eventData);
                Check(!Assets.Behaviour.UI.Shared.UIBlocksZoom.Blocked, "Leaving the sidebar releases its native zoom blocker");
                scroll.verticalNormalizedPosition = 1;
                menu.FocusNode(bModel); b.Dispose();
                yield return null;
                Check(!tabs.Sidebar.Buttons.ContainsKey(beta) && !FoundationApi.TechMods.Any(m => m.Id == beta), "Removing the last node removes its category from the sidebar");
                Check(tabs.SelectedModId != beta && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == tabs.SelectedModId), "Removing the selected category safely selects another populated category");
                Click(tabs.ResearchButton);
                Check(!tabs.Sidebar.Panel.gameObject.activeSelf && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Base Game hides the mod list and retains its original research catalog");
            }
            finally { foreach (var node in registrations.AsEnumerable().Reverse()) node.Dispose(); }
            yield return null;
            tabs.SelectMod(example.Id, false); menu.ShowTechTree(null, true);
        }

        void CheckNoOverlap(TechTreeMenu menu, string name)
        {
            Canvas.ForceUpdateCanvases();
            var parent = TechTreeTabs.Field<RectTransform>(menu, "_nodeParent");
            var rects = menu.Nodes.Select(n => ModResearchLayout.Bounds(n, parent)).ToArray();
            bool clear = true;
            for (int i = 0; i < rects.Length; i++) for (int j = i + 1; j < rects.Length; j++) clear &= !rects[i].Overlaps(rects[j]);
            Check(clear, name);
        }
    }
}
