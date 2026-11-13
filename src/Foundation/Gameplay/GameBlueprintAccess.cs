using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Json;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameBlueprintAccess : IGameBlueprintAccess
    {
        private static readonly AccessTools.FieldRef<WorldFlowManager, WorldFlowGhost> Ghost = AccessTools.FieldRefAccess<WorldFlowManager, WorldFlowGhost>("_currentGhost");
        private static readonly AccessTools.FieldRef<WorldFlowGhost, FlowElement> Element = AccessTools.FieldRefAccess<WorldFlowGhost, FlowElement>("_element");
        private static readonly AccessTools.FieldRef<WorldFlowGhost, bool> Collides = AccessTools.FieldRefAccess<WorldFlowGhost, bool>("_collides");
        private static WorldFlowGhost Require(Component view) => view.GetComponent<WorldFlowGhost>();
        public GameWorkspace DeserializeWorkspace(JsonValue document) => GameWorkspace.Wrap(FlowDiagram.FromJson(document.Native));
        public Component CurrentGhost => WorldFlowManager.Instance ? Ghost(WorldFlowManager.Instance) : null;

        public Component CreateChild(Component parent) => global::UnityEngine.Object.Instantiate(WorldFlowManager.Instance.BlueprintPrefab, parent.transform);
        public void SetBuilding(Component ghost, WorkspaceObject item, bool mirror = false) => Require(ghost).SetElement((FlowElement)item.Native, mirror);
        public void SetDecoration(Component ghost, WorkspaceObject decoration, bool locked = false, bool showCost = false) => Require(ghost).SetDeco((FlowDeco)decoration.Native, locked, showCost);
        public void SetChildOffset(Component ghost, Vector2Int origin) => Require(ghost).SetChildOffset(origin);
        public IReadOnlyList<Component> Children(Component root) => root.GetComponentsInChildren<WorldFlowGhost>().Cast<Component>().ToArray();
        public WorkspaceObject Building(Component ghost)
        {
            var element = Element(Require(ghost));
            return element == null ? null : GameWorkspaceAccess.IdentityFor(element);
        }

        public void SetCollision(Component ghost, bool collides) => Collides(Require(ghost)) = collides;
        public bool Overlaps(Vector2Int position, Vector2Int size) => FlowColliderManager.Instance.Overlaps(position, size);
        public void UpdateDragPosition() => DragDropManager.Instance.UpdateDragPosition();
        public void UpdateCostDisplay(GameMoney cost) => WorldFlowManager.Instance.UpdateCostDisplay(cost.Native);
    }
}
