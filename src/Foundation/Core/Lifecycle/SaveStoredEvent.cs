namespace KingdomFoundation.Core
{
    /// <summary>Provides the save stored event API.</summary>
    public sealed class SaveStoredEvent
    {
        public string SaveName { get; }
        public SaveStoredEvent(string name) { SaveName = name; }
    }
}
