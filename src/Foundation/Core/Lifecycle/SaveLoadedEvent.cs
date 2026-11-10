namespace KingdomFoundation.Core
{
    /// <summary>Provides the save loaded event API.</summary>
    public sealed class SaveLoadedEvent
    {
        public static readonly SaveLoadedEvent Instance = new SaveLoadedEvent();
        private SaveLoadedEvent() { }
    }
}
