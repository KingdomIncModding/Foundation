namespace KingdomFoundation.Core
{
    /// <summary>Provides the save loading event API.</summary>
    public sealed class SaveLoadingEvent
    {
        public static readonly SaveLoadingEvent Instance = new SaveLoadingEvent();
        private SaveLoadingEvent() { }
    }
}
