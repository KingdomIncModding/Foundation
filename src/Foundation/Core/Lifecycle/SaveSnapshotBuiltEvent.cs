namespace KingdomFoundation.Core
{
    /// <summary>Provides the save snapshot built event API.</summary>
    public sealed class SaveSnapshotBuiltEvent
    {
        public static readonly SaveSnapshotBuiltEvent Instance = new SaveSnapshotBuiltEvent();
        private SaveSnapshotBuiltEvent() { }
    }
}
