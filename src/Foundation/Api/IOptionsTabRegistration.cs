using System;
namespace KingdomFoundation.Api
{
    public interface IOptionsTabRegistration : IDisposable
    {
        IOptionsTab Tab { get; }
        bool IsRegistered { get; }
        void Refresh();
    }
}
