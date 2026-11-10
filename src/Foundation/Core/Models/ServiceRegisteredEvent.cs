namespace KingdomFoundation.Core
{
    /// <summary>Provides the service registered event API.</summary>
    public sealed class ServiceRegisteredEvent
    {
        public RegisteredServiceInfo Service { get; }
        public ServiceRegisteredEvent(RegisteredServiceInfo service) { Service = service; }
    }
}
