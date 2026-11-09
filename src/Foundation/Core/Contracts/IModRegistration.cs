using System;

namespace KingdomFoundation.Core
{
    public interface IModRegistration : IDisposable
    {
        ModDescriptor Descriptor { get; }
        ModState State { get; }
        string Message { get; }
    }
}
