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
    /// <summary>Workspace edits routed through the game's manager, never a parallel simulation.</summary>
    public interface IWorkspaceEditorService
    {
        bool TryPurchase(FlowElement element, Vector2Int position);
        void AddWithoutPurchase(FlowElement element, Vector2Int position);
        void Remove(FlowElement element, bool grantNativeRefund = false);
        void Move(FlowElement element, Vector2Int position);
        /// <summary>Uses WorldFlowManager.MoveToFlow, not a copy of native graph state.</summary>
        void MoveToFlow(FlowElement element, int destinationFlowIndex);
        void MoveDecorationToFlow(FlowDeco decoration, int destinationFlowIndex);

        FlowDeco AddDecoration(FlowDeco decoration);
        void RemoveDecoration(FlowDeco decoration);
        bool TryCraft(FlowElement element);
        bool TrySetUpgrade(FlowElement element, int slot, FlowUpgrade upgrade, bool payCost = true);
        IReadOnlyList<FlowUpgrade> GetAvailableUpgrades(FlowElement element);
    }
}
