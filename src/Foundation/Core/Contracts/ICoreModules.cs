using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore modules API.</summary>
    public interface ICoreModules
    {
        IReadOnlyList<ModSnapshot> Registered { get; }
        IModRegistration Register(IFoundationModule module);
        bool TryGet(string modId, out ModSnapshot snapshot);
        void Unregister(string modId);
    }
}
