namespace KingdomFoundation.Core
{
    public sealed class ContentRegisteredEvent
    {
        public ContentDescriptor Content { get; }
        public ContentRegisteredEvent(ContentDescriptor content) { Content = content; }
    }
}
