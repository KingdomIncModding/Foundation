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
    /// <summary>Live, native kingdom and workspace access. Call from the Unity game thread.</summary>
    public interface IGameWorldService
    {
        GamePlayer Player { get; }
        bool IsInKingdom { get; }
        int GameTime { get; }
        FlowDiagram ActiveFlow { get; }
        IReadOnlyList<FlowDiagram> Flows { get; }
        FlowDiagram GetFlow(int index);
        int ActiveFlowIndex { get; }
        int NativeMachineCount(FlowDiagram flow);
        int NativePipeCount(FlowDiagram flow);
        int NativeDecorationCount(FlowDiagram flow);
        IReadOnlyList<FlowElement> ElementsIn(FlowDiagram flow);

        FlowElement FindElement(string guid);
        IReadOnlyList<FlowElement> FindElements(Func<FlowElement, bool> predicate = null);
        void SelectFlow(int index);
        bool TryBuyFlow(out FlowDiagram created);
        void RenameFlow(int index, string label, string iconName = null);
        void SetCamera(Vector2Int position);
    }
}
