namespace KingdomFoundation.Core
{
    public sealed class ServiceRegisteredEvent
    {
        public RegisteredServiceInfo Service { get; }
        public ServiceRegisteredEvent(RegisteredServiceInfo service) { Service = service; }
    }
}
