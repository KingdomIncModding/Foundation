using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class WorkspaceEditorService : IWorkspaceEditorService
    {
        static void RequireInActiveFlow(FlowElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var flow = GameWorldService.RequireManager().CurrentFlow;
            if (!flow.Elements.Contains(element)) throw new InvalidOperationException("Element is not in the active workspace.");
        }
        public bool TryPurchase(FlowElement element, Vector2Int position)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var player = GameWorldService.RequirePlayer();
            var manager = GameWorldService.RequireManager();
            if (element.ParentFlow != null) throw new InvalidOperationException("Element is already placed.");
            if (!element.IsPurchaseable(player)) return false;
            var cost = element.GetPurchaseCost();
            if (!player.SpendCurrency(cost)) return false;
            try { AddWithoutPurchase(element, position); return true; }
            catch { player.AddCurrency(cost, false); throw; }
        }
        public void AddWithoutPurchase(FlowElement element, Vector2Int position)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var manager = GameWorldService.RequireManager();
            var player = GameWorldService.RequirePlayer();
            if (player.Flows.Any(f => f.Elements.Contains(element)))
                throw new InvalidOperationException("Element is already placed in a kingdom flow.");
            element.Position = position;
            manager.CreateNewElement(element);
        }
        public void Remove(FlowElement element, bool grantNativeRefund = false)
        {
            RequireInActiveFlow(element);
            var refund = grantNativeRefund ? element.GetRefund() : null;
            GameWorldService.RequireManager().RemoveElement(element);
            if (refund != null) GameWorldService.RequirePlayer().AddCurrency(refund, false);
        }
        public void Move(FlowElement element, Vector2Int position)
        {
            RequireInActiveFlow(element);
            if (element.World) element.World.UpdatePosition(position);
            else
            {
                element.Position = position;
                FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.Moved,
                    GameWorldService.RequirePlayer().ActiveFlow, element, GameWorldService.RequirePlayer().CurrentFlow));
            }
        }
        public void MoveToFlow(FlowElement element, int destinationFlowIndex)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var player = GameWorldService.RequirePlayer();
            var manager = GameWorldService.RequireManager();
            if (destinationFlowIndex < 0 || destinationFlowIndex >= player.Flows.Count)
                throw new ArgumentOutOfRangeException(nameof(destinationFlowIndex));
            int origin = player.Flows.FindIndex(flow => flow.Elements.Contains(element));
            if (origin < 0) throw new InvalidOperationException("Machine is not in a native workspace.");
            if (origin == destinationFlowIndex) return;
            if (!element.World) throw new InvalidOperationException("Machine native world view is unavailable.");
            // A connected machine requires the game's existing connection editing
            // workflow first; moving it alone would orphan connected native pipes.
            if (element.IO.Any(io => io.ConnectedPipe != null))
                throw new InvalidOperationException("Disconnect native pipes before moving the machine to another workspace.");
            manager.MoveToFlow(element.World, origin, destinationFlowIndex);
        }
        public void MoveDecorationToFlow(FlowDeco decoration, int destinationFlowIndex)
        {
            if (decoration == null) throw new ArgumentNullException(nameof(decoration));
            var player = GameWorldService.RequirePlayer();
            var manager = GameWorldService.RequireManager();
            if (destinationFlowIndex < 0 || destinationFlowIndex >= player.Flows.Count)
                throw new ArgumentOutOfRangeException(nameof(destinationFlowIndex));
            int origin = player.Flows.FindIndex(flow => flow.Decorations.Contains(decoration));
            if (origin < 0) throw new InvalidOperationException("Decoration is not in a native workspace.");
            if (origin == destinationFlowIndex) return;
            if (!decoration.World) throw new InvalidOperationException("Decoration native world view is unavailable.");
            manager.MoveToFlow(decoration.World, origin, destinationFlowIndex);
        }

        public FlowDeco AddDecoration(FlowDeco decoration)
        {
            if (decoration == null) throw new ArgumentNullException(nameof(decoration));
            var flow = GameWorldService.RequireManager().CurrentFlow;
            if (flow.Decorations.Contains(decoration)) throw new InvalidOperationException("Decoration is already placed.");
            GameWorldService.RequireManager().CreateNewDeco(decoration);
            return decoration;
        }
        public void RemoveDecoration(FlowDeco decoration)
        {
            if (decoration == null) throw new ArgumentNullException(nameof(decoration));
            if (!GameWorldService.RequireManager().CurrentFlow.Decorations.Contains(decoration))
                throw new InvalidOperationException("Decoration is not in the active flow.");
            if (!decoration.World) throw new InvalidOperationException("Decoration's native world view is unavailable.");
            GameWorldService.RequireManager().RemoveDecoration(decoration.World);
        }
        public bool TryCraft(FlowElement element)
        {
            RequireInActiveFlow(element);
            if (!element.CanManualCraft()) return false;
            element.DoManualCraft();
            return true;
        }
        public IReadOnlyList<FlowUpgrade> GetAvailableUpgrades(FlowElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return Array.AsReadOnly(element.GetAvailableUpgrades().ToArray());
        }
        public bool TrySetUpgrade(FlowElement element, int slot, FlowUpgrade upgrade, bool payCost = true)
        {
            RequireInActiveFlow(element);
            if (slot < 0 || slot >= element.UpgradeSlots) throw new ArgumentOutOfRangeException(nameof(slot));
            if (upgrade != null && !upgrade.IsAvailableFor(element)) return false;
            var player = GameWorldService.RequirePlayer();
            var old = element.GetUpgrade(slot);
            var cost = upgrade == null || !payCost ? null : upgrade.GetPurchaseCost(element);
            if (cost != null && !player.SpendCurrency(cost)) return false;
            try
            {
                element.SetUpgrade(slot, upgrade);
                if (element.World) element.World.UpdateUpgradeSlots();
                return true;
            }
            catch
            {
                element.SetUpgrade(slot, old);
                if (cost != null) player.AddCurrency(cost, false);
                throw;
            }
        }
    }
}
