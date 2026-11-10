namespace KingdomFoundation.Core
{
    /// <summary>Provides the ifoundation module API.</summary>
    public interface IFoundationModule
    {
        ModDescriptor Descriptor { get; }
        void Initialize(IModContext context);
        void Shutdown();
    }
}
