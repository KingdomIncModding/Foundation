using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    internal sealed class OwnedPatches : IModPatches
    {
        readonly string owner;
        readonly CorePatchManager patches;
        readonly ModScope scope;
        internal OwnedPatches(string owner, CorePatchManager patches, ModScope scope)
        { this.owner = owner; this.patches = patches; this.scope = scope; }
        /// <summary>Performs the patch class operation.</summary>
        public void PatchClass(Type patchContainer)
        {
            scope.EnsureActive();
            patches.PatchClass(owner, patchContainer);
        }
        public IReadOnlyList<PatchConflict> Conflicts()
        {
            scope.EnsureActive();
            return patches.Conflicts(owner);
        }
    }
}
