namespace KingdomFoundation.Core
{
    public sealed class ContentReleasedEvent
    {
        public ContentDescriptor Content { get; }
        public ContentReleasedEvent(ContentDescriptor content) { Content = content; }
    }
}
