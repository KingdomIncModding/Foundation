namespace KingdomFoundation.Core
{
    /// <summary>Provides the content released event API.</summary>
    public sealed class ContentReleasedEvent
    {
        public ContentDescriptor Content { get; }
        public ContentReleasedEvent(ContentDescriptor content) { Content = content; }
    }
}
