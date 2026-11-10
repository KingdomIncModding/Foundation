using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the icore patch manager API.</summary>
    public interface ICorePatchManager
    {
        void PatchClass(string ownerId, Type patchContainer);
        IReadOnlyList<PatchConflict> Conflicts(string ownerId);
        void ReleaseOwner(string ownerId);
    }
}
