namespace KingdomFoundation.Core
{
    public sealed class ServiceRemovedEvent
    {
        public RegisteredServiceInfo Service { get; }
        public ServiceRemovedEvent(RegisteredServiceInfo service) { Service = service; }
    }
}
