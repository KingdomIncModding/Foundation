namespace KingdomFoundation.Api
{
    public interface IGameDragDropService
    {
        bool IsReady { get; }
        IGameDragDropTarget Target { get; }
        bool SetTarget(IGameDragDropTarget target, bool holdMode);
        void Clear();
    }
}
