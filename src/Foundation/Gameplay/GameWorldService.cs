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
    internal sealed class GameWorldService : IGameWorldService
    {
        public GamePlayer Player => GamePlayer.Current;
        public bool IsInKingdom => GamePlayer.Current != null && GamePlayer.Current.Flows.Count != 0;
        public int GameTime => RequirePlayer().GameTime;
        public FlowDiagram ActiveFlow => IsInKingdom ? RequirePlayer().ActiveFlow : null;
        public int ActiveFlowIndex => RequirePlayer().CurrentFlow;
        public int NativeMachineCount(FlowDiagram flow) => (flow ?? throw new ArgumentNullException(nameof(flow))).Elements.Count;
        public int NativePipeCount(FlowDiagram flow) => (flow ?? throw new ArgumentNullException(nameof(flow))).Pipes.Count;
        public int NativeDecorationCount(FlowDiagram flow) => (flow ?? throw new ArgumentNullException(nameof(flow))).Decorations.Count;
        public IReadOnlyList<FlowElement> ElementsIn(FlowDiagram flow)
            => Array.AsReadOnly((flow ?? throw new ArgumentNullException(nameof(flow))).Elements.ToArray());

        public IReadOnlyList<FlowDiagram> Flows => GamePlayer.Current == null
            ? Array.Empty<FlowDiagram>() : Array.AsReadOnly(GamePlayer.Current.Flows.ToArray());

        internal static GamePlayer RequirePlayer()
        {
            return GamePlayer.Current ?? throw new InvalidOperationException("There is no active kingdom.");
        }
        internal static WorldFlowManager RequireManager()
        {
            var manager = WorldFlowManager.Instance;
            if (!manager) throw new InvalidOperationException("The native workspace manager is not ready.");
            RequirePlayer();
            return manager;
        }
        public FlowDiagram GetFlow(int index)
        {
            var player = RequirePlayer();
            if (index < 0 || index >= player.Flows.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return player.Flows[index];
        }
        public FlowElement FindElement(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var player = GamePlayer.Current;
            if (player == null) return null;
            foreach (var diagram in player.Flows)
                foreach (var element in diagram.Elements)
                    if (element != null && string.Equals(element.Guid, guid, StringComparison.Ordinal)) return element;
            return null;
        }
        public IReadOnlyList<FlowElement> FindElements(Func<FlowElement, bool> predicate = null)
        {
            var player = GamePlayer.Current;
            if (player == null) return Array.Empty<FlowElement>();
            var results = new List<FlowElement>();
            foreach (var diagram in player.Flows)
                foreach (var element in diagram.Elements)
                    if (element != null && (predicate == null || predicate(element))) results.Add(element);
            return results.AsReadOnly();
        }
        public void SelectFlow(int index)
        {
            GetFlow(index);
            RequireManager().ShowFlow(index);
        }
        public bool TryBuyFlow(out FlowDiagram created)
        {
            created = null;
            var player = RequirePlayer();
            var manager = RequireManager();
            var price = player.GetExtraWorkflowCost();
            if (!player.SpendCurrency(price)) return false;
            var next = new FlowDiagram();
            player.Flows.Add(next);
            try
            {
                if (GameUI.Instance) GameUI.Instance.UpdateFlowMenu(true);
                manager.ShowFlow(player.Flows.Count - 1);
                created = next;
                FoundationApi.Events.Publish(new KingdomFlowCreatedEvent(next, player.Flows.Count - 1));
                return true;
            }
            catch
            {
                player.Flows.Remove(next);
                player.AddCurrency(price, false);
                throw;
            }
        }
        public void RenameFlow(int index, string label, string iconName = null)
        {
            var flow = GetFlow(index);
            if (label == null) throw new ArgumentNullException(nameof(label));
            flow.Label = label;
            if (iconName != null) flow.IconName = iconName;
            if (GameUI.Instance) GameUI.Instance.UpdateFlowMenu();
        }
        public void SetCamera(Vector2Int position)
        {
            var player = RequirePlayer();
            player.FlowCamera = position;
            // The native camera component owns its visual position; ShowFlow applies the saved coordinate.
            RequireManager().ShowFlow(player.CurrentFlow);
        }
    }
}
