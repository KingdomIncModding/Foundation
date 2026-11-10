using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the imod patches API.</summary>
    public interface IModPatches
    {
        void PatchClass(Type patchContainer);
        IReadOnlyList<PatchConflict> Conflicts();
    }
}
