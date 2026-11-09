namespace KingdomFoundation.Core
{
    public sealed class SaveSnapshotBuiltEvent
    {
        public static readonly SaveSnapshotBuiltEvent Instance = new SaveSnapshotBuiltEvent();
        private SaveSnapshotBuiltEvent() { }
    }
}
