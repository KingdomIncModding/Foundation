namespace KingdomFoundation.Core
{
    public interface IFoundationModule
    {
        ModDescriptor Descriptor { get; }
        void Initialize(IModContext context);
        void Shutdown();
    }
}
