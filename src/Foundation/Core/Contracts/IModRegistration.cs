using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the imod registration API.</summary>
    public interface IModRegistration : IDisposable
    {
        ModDescriptor Descriptor { get; }
        ModState State { get; }
        string Message { get; }
    }
}
