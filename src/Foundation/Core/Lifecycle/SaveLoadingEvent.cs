namespace KingdomFoundation.Core
{
    public sealed class SaveLoadingEvent
    {
        public static readonly SaveLoadingEvent Instance = new SaveLoadingEvent();
        private SaveLoadingEvent() { }
    }
}
