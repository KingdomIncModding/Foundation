namespace KingdomFoundation.Core
{
    /// <summary>Provides the content registered event API.</summary>
    public sealed class ContentRegisteredEvent
    {
        public ContentDescriptor Content { get; }
        public ContentRegisteredEvent(ContentDescriptor content) { Content = content; }
    }
}
