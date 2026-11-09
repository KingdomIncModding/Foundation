using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    public interface IModPatches
    {
        void PatchClass(Type patchContainer);
        IReadOnlyList<PatchConflict> Conflicts();
    }
}
