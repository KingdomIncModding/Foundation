using System;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    internal sealed class CoreLifecycleInstaller
    {
        readonly CoreDiagnostics diagnostics;
        readonly CorePatchManager patches;
        internal CoreLifecycleInstaller(CoreDiagnostics diagnostics, CorePatchManager patches)
        { this.diagnostics = diagnostics; this.patches = patches; }
        internal void Install()
        {
            foreach (var type in new[] { typeof(SaveSnapshotPatch), typeof(SaveStorePatch), typeof(SaveLoadPatch) })
            {
                try { patches.PatchClass("foundation", type); }
                catch (Exception ex)
                {
                    diagnostics.Record(CoreSeverity.Error, "foundation", "save-hook", "Failed to register a native save observation hook.", ex);
                }
            }
        }
    }
}
