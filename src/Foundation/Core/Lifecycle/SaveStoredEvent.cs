namespace KingdomFoundation.Core
{
    public sealed class SaveStoredEvent
    {
        public string SaveName { get; }
        public SaveStoredEvent(string name) { SaveName = name; }
    }
}
