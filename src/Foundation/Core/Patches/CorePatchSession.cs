using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    internal sealed class CorePatchSession
    {
        readonly Harmony harmony;
        readonly HashSet<Type> applied = new HashSet<Type>();
        internal string HarmonyId { get; }
        internal CorePatchSession(string modId)
        {
            HarmonyId = "urakka.kingdominc.foundation.core." + modId;
            harmony = new Harmony(HarmonyId);
        }
        internal void PatchClass(Type patchContainer)
        {
            if (patchContainer == null) throw new ArgumentNullException(nameof(patchContainer));
            if (!applied.Add(patchContainer)) throw new InvalidOperationException("Patch already applied: " + patchContainer.FullName);
            try { harmony.CreateClassProcessor(patchContainer).Patch(); }
            catch
            {
                // Preserve owner and other successful containers. Harmony is not
                // transactional: any partial patch conflict is surfaced through
                // Core.Diagnostics and must be examined before retrying.
                applied.Remove(patchContainer);
                throw;
            }
        }
        internal void Release()
        {
            harmony.UnpatchSelf();
            applied.Clear();
        }
    }
}
