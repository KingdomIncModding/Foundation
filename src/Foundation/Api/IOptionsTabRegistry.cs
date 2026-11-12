using System;
using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    /// <summary>Main-thread Options extensions. No vanilla visibility preferences are stored by this registry.</summary>
    public interface IOptionsTabRegistry
    {
        IReadOnlyList<IOptionsTabRegistration> Tabs { get; }
        IReadOnlyList<IOptionsView> OpenViews { get; }
        event Action Changed;
        IOptionsTabRegistration Register(IOptionsTab tab);
        bool TryGet(string modId, string id, out IOptionsTabRegistration registration);
        /// <summary>Runs on each Options opening and immediately for already-open views. Dispose to unsubscribe.</summary>
        IDisposable ObserveViews(Action<IOptionsView> opened);
    }
}
