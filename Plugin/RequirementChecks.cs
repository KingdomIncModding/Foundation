using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        IEnumerator VerifyRequirementNavigation(TechTreeMenu menu, TechTreeTabs tabs, Camera camera, string directory)
        {
            long gold = GamePlayer.Current.GetCurrency(Currency.Gold);
            menu.FocusNode(TechNode.Get("supply_depot"));
            yield return null;
            var native = Node(menu, "supply_depot"); native.UpdateNodeStatus();
            var label = TechTreeTabs.Field<TMP_Text>(native, "_requirementsText");
            label.ForceMeshUpdate();
            Check(label.textInfo.linkCount == 1 && label.GetParsedText().Contains("Requires"), "Original requirement label retains native translated text with a clickable target");
            camera.transform.position = new Vector3(native.Tech.Position.x, native.Tech.Position.y, -10);
            yield return null;
            var position = camera.transform.position;
            RequirementClick(native, camera, 0, PointerEventData.InputButton.Right);
            Check(menu.RootNode == null && camera.transform.position == position, "Right clicking a requirement does not navigate");
            RequirementClick(native, camera, 0);
            var automation = TechNode.Get("mine_automation");
            Check(!tabs.ModsSelected && menu.RootNode == automation.Root && menu.Nodes.Any(n => n.Tech == automation), "Native prerequisite click opens its correct Base Game branch");
            CheckFocused(camera, automation, "Native prerequisite click centers the destination");

            using (var origin = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.origin", "Origin Technology")))
            using (var source = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.requires", "Requirement Navigation", new Vector2(1300, 0),
                research: new TechNodeOptions(requirements: new[] { "requisition_office", "urakka.foundation.example.upgrade", origin.Id }))))
            {
                var sourceModel = ((INativeTechNode)source).NativeNode;
                menu.FocusNode(sourceModel);
                yield return null;
                var view = Node(menu, source.Id); view.UpdateNodeStatus();
                label = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText"); label.ForceMeshUpdate();
                Check(label.textInfo.linkCount == 3, "Each missing prerequisite gets its own destination");
                Capture(camera, System.IO.Path.Combine(directory, "foundation-requirement-links.png"));
                RequirementClick(view, camera, 0);
                var requisition = TechNode.Get("requisition_office");
                Check(!tabs.ModsSelected && menu.RootNode == requisition.Root && menu.Nodes.Any(n => n.Tech == requisition), "Requires Requisitionist click switches to Base Game and shows Requisitionist");
                CheckFocused(camera, requisition, "Requires Requisitionist centers its actual research node");
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id); view.UpdateNodeStatus();
                RequirementClick(view, camera, 1);
                var upgrade = TechNode.Get("urakka.foundation.example.upgrade");
                Check(tabs.ModsSelected && menu.RootNode == upgrade.Root && menu.Nodes.Any(n => n.Tech == upgrade), "Second requirement opens the correct Mods upgrade branch");
                CheckFocused(camera, upgrade, "Second requirement centers the correct node rather than the first requirement");
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id); view.UpdateNodeStatus();
                RequirementClick(view, camera, 2);
                Check(tabs.ModsSelected && menu.RootNode == null, "Requirement navigation supports a framework root at the default origin");
                CheckFocused(camera, ((INativeTechNode)origin).NativeNode, "Default-origin requirement centers without a null parent");
                Check(GamePlayer.Current.GetCurrency(Currency.Gold) == gold && !sourceModel.IsPurchased && !upgrade.IsPurchased && !requisition.IsPurchased,
                    "Requirement navigation purchases no research and spends no gold");
                GamePlayer.Current.AddTech(origin.Id);
                menu.FocusNode(sourceModel);
                yield return null;
                view = Node(menu, source.Id); view.UpdateNodeStatus();
                label = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText"); label.ForceMeshUpdate();
                Check(label.textInfo.linkCount == 2 && !label.GetParsedText().Contains("Origin Technology"), "Purchased prerequisites disappear and remaining link destinations refresh");
                RequirementClick(view, camera, 1);
                Check(menu.RootNode == upgrade.Root, "Remaining second requirement keeps the correct destination after purchase");
            }
        }

        void CheckFocused(Camera camera, TechNode node, string name)
        {
            var point = node.Position == Vector2.zero && node.Root != null ? node.Root.Position : node.Position;
            Check(Vector3.Distance(camera.transform.position, new Vector3(point.x, point.y, -10)) < .01f, name);
        }

        void RequirementClick(UITechNode view, Camera camera, int linkIndex, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            Canvas.ForceUpdateCanvases();
            var text = TechTreeTabs.Field<TMP_Text>(view, "_requirementsText"); text.ForceMeshUpdate();
            var link = text.textInfo.linkInfo[linkIndex];
            var character = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength / 2];
            var world = text.transform.TransformPoint((character.bottomLeft + character.topRight) * .5f);
            var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camera, world), button = button };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            var hit = hits.FirstOrDefault();
            Check(hit.gameObject && hit.gameObject.GetComponentInParent<RequirementLinks>(), "Requirement label receives the first pointer raycast: " + view.Tech.Identifier + " / " + linkIndex);
            data.pointerPressRaycast = hit; data.pointerCurrentRaycast = hit;
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
    }
}
