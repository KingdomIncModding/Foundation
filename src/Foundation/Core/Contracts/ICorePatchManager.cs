using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    public interface ICorePatchManager
    {
        void PatchClass(string ownerId, Type patchContainer);
        IReadOnlyList<PatchConflict> Conflicts(string ownerId);
        void ReleaseOwner(string ownerId);
    }
}
