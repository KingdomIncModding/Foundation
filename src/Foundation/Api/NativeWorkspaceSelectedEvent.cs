using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Post-commit observation of native WorldFlowManager.ShowFlow.</summary>
    public sealed class NativeWorkspaceSelectedEvent
    {
        public GamePlayer Player { get; }
        public int PreviousIndex { get; }
        public int CurrentIndex { get; }
        public FlowDiagram CurrentFlow { get; }
        public NativeWorkspaceSelectedEvent(GamePlayer player, int previous, int current, FlowDiagram flow)
        { Player = player; PreviousIndex = previous; CurrentIndex = current; CurrentFlow = flow; }
    }
}
