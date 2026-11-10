namespace KingdomFoundation.Core
{
    /// <summary>Provides the service removed event API.</summary>
    public sealed class ServiceRemovedEvent
    {
        public RegisteredServiceInfo Service { get; }
        public ServiceRemovedEvent(RegisteredServiceInfo service) { Service = service; }
    }
}
