using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace KingdomFoundation.Core
{
    internal sealed class CorePatchManager : ICorePatchManager
    {
        readonly object gate = new object();
        readonly Dictionary<string, CorePatchSession> sessions = new Dictionary<string, CorePatchSession>(StringComparer.Ordinal);
        readonly CoreDiagnostics diagnostics;
        internal CorePatchManager(CoreDiagnostics diagnostics) { this.diagnostics = diagnostics; }
        public void PatchClass(string ownerId, Type patchContainer)
        {
            ModIdentity.Validate(ownerId);
            if (patchContainer == null) throw new ArgumentNullException(nameof(patchContainer));
            CorePatchSession session;
            bool created = false;
            lock (gate)
            {
                if (!sessions.TryGetValue(ownerId, out session))
                {
                    sessions.Add(ownerId, session = new CorePatchSession(ownerId));
                    created = true;
                }
            }
            try { lock (session) session.PatchClass(patchContainer); }
            catch (Exception error)
            {
                if (created)
                {
                    // A first-time patch failure may have applied some Harmony
                    // methods before throwing. Remove this new owner's session
                    // and unpatch only its own Harmony ID, never another mod.
                    lock (gate)
                        if (sessions.TryGetValue(ownerId, out var live) && ReferenceEquals(live, session))
                            sessions.Remove(ownerId);
                    try { lock (session) session.Release(); }
                    catch (Exception rollbackError)
                    {
                        diagnostics.Record(CoreSeverity.Error, ownerId, "harmony-rollback",
                            "Failed to release partially applied patch container.", rollbackError);
                    }
                }
                diagnostics.Record(CoreSeverity.Error, ownerId, "harmony", "Could not apply patch container: " + patchContainer.FullName, error);
                throw;
            }
        }
        public IReadOnlyList<PatchConflict> Conflicts(string ownerId)
        {
            ModIdentity.Validate(ownerId);
            var harmonyId = "urakka.kingdominc.foundation.core." + ownerId;
            var result = new List<PatchConflict>();
            foreach (var method in Harmony.GetAllPatchedMethods())
            {
                var info = Harmony.GetPatchInfo(method);
                if (info == null) continue;
                var all = info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).ToArray();
                if (!all.Any(p => p.owner == harmonyId)) continue;
                foreach (var other in all.Where(p => p.owner != harmonyId).Select(p => p.owner).Distinct(StringComparer.Ordinal))
                    result.Add(new PatchConflict(method.DeclaringType?.FullName + "." + method.Name, ownerId, other,
                        "shared native patch target"));
            }
            return result.AsReadOnly();
        }
        public void ReleaseOwner(string ownerId)
        {
            CorePatchSession session;
            lock (gate)
            {
                if (!sessions.TryGetValue(ownerId, out session)) return;
                sessions.Remove(ownerId);
            }
            try { lock (session) session.Release(); }
            catch (Exception error) { diagnostics.Record(CoreSeverity.Error, ownerId, "unpatch", "Could not unpatch owner.", error); }
        }
        internal void ReleaseAll()
        {
            string[] owners;
            lock (gate) owners = sessions.Keys.ToArray();
            foreach (var owner in owners) ReleaseOwner(owner);
        }
    }
}
