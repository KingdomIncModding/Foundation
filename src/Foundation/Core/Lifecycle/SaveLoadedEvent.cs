namespace KingdomFoundation.Core
{
    public sealed class SaveLoadedEvent
    {
        public static readonly SaveLoadedEvent Instance = new SaveLoadedEvent();
        private SaveLoadedEvent() { }
    }
}
