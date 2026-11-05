using System;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        const string RelayId = "urakka.foundation.example.relay";
        IWorkspaceNodeType relayType;
        WorkspaceNode relayA, relayB;
        WorldFlowGhost relayGhost;
        FlowPipe relayPipe;
        Keyboard workspaceKeyboard;
        long workspaceGold;
        int workspaceCount;
        void VerifyWorkspaceRegistry()
        {
            var registry = FoundationApi.WorkspaceNodes;
            int registeredCount = registry.Nodes.Count;
            Check(registry.Nodes.Count(n => n.Id == RelayId) == 1 && registry.TryGet(RelayId, out relayType), "Separate consumer registers a workspace node type through public interfaces");
            Throws<ArgumentNullException>(() => registry.RegisterWorkspaceNode(null), "Null workspace definitions rejected");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(relayType.Definition), "Duplicate workspace IDs rejected without replacing owner");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("Mine", "Conflict", () => null, "mine")), "Workspace registration cannot replace a native building");
            Throws<ArgumentException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("diagnostic.unknown", "Unknown", () => null, "missing.research")), "Unknown required research rejected");
            Throws<InvalidOperationException>(() => registry.RegisterWorkspaceNode(new WorkspaceNodeDefinition("diagnostic.null_factory", "Invalid", () => null, "mine")), "Null factory result rejected and registration rolled back");
            Check(!registry.TryGet("diagnostic.null_factory", out _) && registry.Nodes.Count == registeredCount, "Failed factory leaves registry unchanged");
            var a = relayType.CreateInstance(); var b = relayType.CreateInstance();
            Check(a != b && a.Guid != b.Guid && a.ElementType == RelayId && b.ElementType == RelayId, "Workspace factory creates independent models with stable type IDs and fresh GUIDs");
            Check(a is FlowElement && a.Label == "Example Relay" && a.Inputs.Count == 1 && a.Outputs.Count == 1, "External WorkspaceNode subclass is a native model with native ports");
        }
        void VerifyWorkspaceLocked()
        {
            Check(FlowElement.GetAllTypes().Count(n => n.ElementType == RelayId) == 1, "Native catalog includes custom workspace type exactly once");
            Check(!FlowElement.GetBuildableTypes().Any(n => n.ElementType == RelayId), "Native build menu hides workspace type before its research is unlocked");
            Throws<InvalidOperationException>(() => relayType.BeginPlacement(), "Programmatic blueprint placement also respects research lock");
            var model = FlowElement.Create(RelayId);
            Check(model is WorkspaceNode && model.RequiredTech == TechNode.Get("urakka.foundation.example.first"), "Native factory resolves custom type and required research");
            Check(FlowElement.Create("Mine").ElementType == "Mine", "Original native factory still resolves original game buildings");
        }
        void PrepareWorkspacePlacement()
        {
            Check(FlowElement.GetBuildableTypes().Count(n => n.ElementType == RelayId) == 1, "Research unlock exposes custom type in native build catalog");
            workspaceKeyboard = InputSystem.AddDevice<Keyboard>(); workspaceKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(workspaceKeyboard, new KeyboardState()); InputSystem.Update();
            GameUI.Instance.ToggleBuyMenu(true);
            var menu = FindObjectsByType<ElementBuyMenu>(FindObjectsSortMode.None).Single(n => n.isActiveAndEnabled);
            menu.ShowCategory(ElementCategory.Utility);
            var row = menu.GetComponentsInChildren<BuyMenuRow>().Last(r => TechTreeTabs.Field<FlowElement>(r, "_element").ElementType == RelayId);
            Check(TechTreeTabs.Field<TMPro.TMP_Text>(row, "_label").text == "Example Relay", "Native buy menu renders the workspace node's name");
            workspaceGold = GamePlayer.Current.GetCurrency(Currency.Gold);
            workspaceCount = GamePlayer.Current.ActiveFlow.Elements.Count;
            ExecuteEvents.Execute(row.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            relayGhost = TechTreeTabs.Field<WorldFlowGhost>(WorldFlowManager.Instance, "_currentGhost");
            Check(relayGhost && WorldFlowManager.Instance.BlueprintActive, "Native buy-row click starts the actual blueprint placement ghost");
            Check(((RectTransform)relayGhost.transform).sizeDelta == relayType.Definition.Size, "Native blueprint uses registered node dimensions");
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(-640, 192));
            relayA = (WorkspaceNode)GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == workspaceCount + 1 && GamePlayer.Current.GetCurrency(Currency.Gold) == workspaceGold - 20, "Native placement creates a saved model and charges its purchase cost");
            Check(relayA.Position == new Vector2Int(-640, 192) && relayA.World && relayA.World.Element == relayA, "Placed custom model is bound to the game's WorldFlowElement");
            GameUI.Instance.ToggleBuyMenu(false);
            relayGhost = relayType.BeginPlacement();
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(384, 192));
            relayB = (WorkspaceNode)GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(!ReferenceEquals(relayA, relayB) && relayA.Guid != relayB.Guid && GamePlayer.Current.GetCurrency(Currency.Gold) == workspaceGold - 40, "API placement uses the same native purchase flow for a second independent node");
        }
        void PlaceWorkspaceGhost(WorldFlowGhost ghost, Vector2Int position)
        {
            ghost.OnDragDropMove(position, Vector2Int.zero);
            Physics2D.SyncTransforms(); AccessTools.Method(typeof(WorldFlowGhost), "Update").Invoke(ghost, null);
            DragDropManager.Instance.ClearDragDrop(); ghost.OnDragDropEnd();
        }
        void ExerciseWorkspace()
        {
            var world = relayA.World;
            var template = TechTreeTabs.Field<WorldFlowElement>(WorldFlowManager.Instance, "_defaultPrefab");
            Check(world.GetType() == template.GetType() && world.BackgroundSprite == template.BackgroundSprite, "Workspace node uses the game's actual generic building prefab and background");
            Check(world.GetComponentInChildren<WorldFlowLabel>(true) && world.Collider && world.Collider.Element == world, "Native drag label and collision component remain attached");
            Check(relayA.Inputs[0].World && relayA.Outputs[0].World && relayA.Inputs[0].World.IO == relayA.Inputs[0], "Native input/output widgets bind to the mod's port models");
            var before = GamePlayer.Current.ActiveFlow.Elements.Count;
            var gold = GamePlayer.Current.GetCurrency(Currency.Gold);
            var blocked = relayType.BeginPlacement(); PlaceWorkspaceGhost(blocked, relayA.Position);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == before && GamePlayer.Current.GetCurrency(Currency.Gold) == gold, "Native collision rejection prevents overlapping purchases without charging currency");
            WorldFlowManager.Instance.StopBlueprintMode(); DragDropManager.Instance.ClearDragDrop();
            var label = relayA.World.GetComponentInChildren<WorldFlowLabel>(true);
            var start = relayA.Position;
            AccessTools.Method(typeof(WorldFlowLabel), "_startDragDrop").Invoke(label, new object[] { false });
            label.OnDragDropMove(start + new Vector2Int(64, 64), Vector2Int.zero);
            Check(relayA.Position == start + new Vector2Int(64, 64), "Workspace node moves through native label dragging and snapping");
            label.OnDragDropCancel();
            Check(relayA.Position == start, "Native drag cancellation restores workspace position");
            relayPipe = new FlowPipe(); relayPipe.Connect(relayA.Outputs[0], relayB.Inputs[0]);
            relayPipe.Nodes.Add(new Vector2Int(0, 152)); WorldFlowManager.Instance.CreateNewPipe(relayPipe);
            Check(relayPipe.World && relayA.Outputs[0].ConnectedPipe == relayPipe && relayB.Inputs[0].ConnectedPipe == relayPipe, "Native pipe renderer and models connect registered workspace ports");
            var item = new InventoryItem("iron_ore");
            Check(relayA.Inputs[0].InputItem(item) && !relayA.Inputs[0].InputItem(new InventoryItem("iron_ore")), "Native input buffer enforces its declared capacity");
            relayA.Update(.1f); relayPipe.Update(.1f); relayB.Update(.1f);
            Check(ReferenceEquals(relayB.Outputs[0].NextItem, item) && !relayA.Inputs[0].HasBuffer && !relayA.Outputs[0].HasBuffer, "Separate plugin behavior transfers the original item through native pipe flow");
            relayA.Outputs[0].BufferedItems.Add(new InventoryItem("iron_ore")); relayPipe.Update(.1f);
            relayA.Outputs[0].BufferedItems.Add(new InventoryItem("iron_ore")); relayPipe.Update(.1f);
            Check(relayA.Outputs[0].HasBuffer && relayB.Inputs[0].HasBuffer, "Native backpressure retains buffered items when destination is full");
            relayA.GetType().GetField("Transfers").SetValue(relayA, 7);
            var copy = (WorkspaceNode)relayA.CreateCopy();
            Check(copy.Guid != relayA.Guid && (int)copy.GetType().GetField("Transfers").GetValue(copy) == 7 && !copy.Inputs[0].HasBuffer && !copy.Outputs[0].HasBuffer, "Native copy creates a new identity and invokes external CopySettingsTo without copying inventory");
            var json = GamePlayer.Current.ActiveFlow.ToJson(); var restored = FlowDiagram.FromJson(json);
            var restoredA = restored.Elements.Single(e => e.Guid == relayA.Guid);
            var restoredB = restored.Elements.Single(e => e.Guid == relayB.Guid);
            Check(restoredA is WorkspaceNode && restoredA.Position == relayA.Position && (int)restoredA.GetType().GetField("Transfers").GetValue(restoredA) == 7, "Native save round trip restores custom model, position, GUID and mod-owned state");
            Check(restoredA.Outputs[0].HasBuffer && restoredB.Inputs[0].HasBuffer && restoredB.Outputs[0].HasBuffer, "Native serializer restores workspace port inventories");
            Check(restored.Pipes.Any(p => p.ConnectedOutput.Parent == restoredA && p.ConnectedInput.Parent == restoredB && p.Nodes.SequenceEqual(relayPipe.Nodes)), "Native serializer reconnects workspace pipes and bends by saved GUID/port index");
            var player = GamePlayer.Current;
            try { var saved = GamePlayer.FromJson(player.ToJson()); Check(saved.ActiveFlow.Elements.Count(e => e.ElementType == RelayId) == 2, "Full kingdom JSON round trip restores registered workspace instances"); }
            finally { GamePlayer.Current = player; }
            relayGhost = WorldFlowManager.Instance.ShowPurchaseBlueprint(relayA, false);
            PlaceWorkspaceGhost(relayGhost, new Vector2Int(384, -256));
            var placedCopy = GamePlayer.Current.ActiveFlow.Elements.Last(e => e.ElementType == RelayId);
            Check(GamePlayer.Current.ActiveFlow.Elements.Count == before + 1 && (int)placedCopy.GetType().GetField("Transfers").GetValue(placedCopy) == 7, "Native copy blueprint places the consumer's settings into another independent workspace node");
        }
        void FinishWorkspace(string directory)
        {
            var camera = TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera");
            WorldFlowCamera.Instance.SetZoomLevel(3); camera.transform.position = new Vector3(0, 0, -10);
            GameUI.Instance.ToggleBuyMenu(false);
            Capture(camera, Path.Combine(directory, "foundation-workspace.png"));
            var snapshot = FoundationApi.WorkspaceNodes.Nodes;
            int registeredCount = snapshot.Count;
            var saved = relayB.ToJson();
            relayType.Dispose(); relayType.Dispose();
            Check(!relayType.IsRegistered && snapshot.Count == registeredCount && snapshot.Any(n => n.Id == RelayId) &&
                FoundationApi.WorkspaceNodes.Nodes.Count == registeredCount - 1 && !FoundationApi.WorkspaceNodes.Nodes.Any(n => n.Id == RelayId),
                "Disposal removes building type from palette while keeping stable registry snapshots");
            Check(!FlowElement.GetBuildableTypes().Any(e => e.ElementType == RelayId) && relayB.World, "Disposed type is hidden from purchases while placed nodes stay intact");
            Check(FlowElement.FromJson(saved, new FlowDiagram()).ElementType == RelayId, "Inactive type remains loadable for existing saved instances during the session");
            Throws<ObjectDisposedException>(() => relayType.BeginPlacement(), "Disposed registration cannot start new placement");
            var replacement = FoundationApi.RegisterWorkspaceNode(relayType.Definition);
            relayType.Dispose();
            Check(replacement.IsRegistered && FoundationApi.WorkspaceNodes.Nodes.Count == registeredCount, "Stale registration disposal cannot unregister a replacement");
            WorldFlowManager.Instance.RemoveElement(relayA);
            Check(!GamePlayer.Current.ActiveFlow.Elements.Contains(relayA) && !GamePlayer.Current.ActiveFlow.Pipes.Contains(relayPipe) && relayB.Inputs[0].ConnectedPipe == null, "Native removal disconnects attached workspace pipes without deleting other nodes");
            if (workspaceKeyboard != null) InputSystem.RemoveDevice(workspaceKeyboard);
        }
    }
}
