using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the workspace changed event API.</summary>
    public sealed class WorkspaceChangedEvent
    {
        public WorkspaceChangeKind Kind { get; }
        public FlowDiagram Diagram { get; }
        public FlowElement Element { get; }
        public FlowElementSnapshot Snapshot { get; }
        public int FlowIndex { get; }
        public WorkspaceChangedEvent(WorkspaceChangeKind kind, FlowDiagram diagram, FlowElement element, int flowIndex)
        {
            Kind = kind; Diagram = diagram; Element = element; FlowIndex = flowIndex;
            if (element != null) Snapshot = new FlowElementSnapshot(element);
        }
    }
}
