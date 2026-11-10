using System;
using System.Collections.Generic;

namespace KingdomFoundation.Core
{
    /// <summary>Owns only managed registrations, never native static game content.</summary>
    internal sealed class ModScope : IDisposable
    {
        readonly object gate = new object();
        readonly List<IDisposable> owned = new List<IDisposable>();
        readonly CoreDiagnostics diagnostics;
        readonly string modId;
        bool disposed;
        internal ModScope(string modId, CoreDiagnostics diagnostics)
        { this.modId = modId; this.diagnostics = diagnostics; }
        internal void EnsureActive()
        {
            lock (gate)
                if (disposed) throw new ObjectDisposedException("Foundation mod scope: " + modId);
        }
        internal IDisposable Track(IDisposable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            lock (gate)
            {
                if (!disposed)
                {
                    owned.Add(item);
                    return item;
                }
            }
            // The module may have stopped while a background task was enqueuing.
            // Cancel this just-created token so no orphan callback can execute.
            item.Dispose();
            throw new ObjectDisposedException("Foundation mod scope: " + modId);
        }
        /// <summary>Performs the dispose operation.</summary>
        public void Dispose()
        {
            IDisposable[] snapshot;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                snapshot = owned.ToArray();
                owned.Clear();
            }
            for (int i = snapshot.Length - 1; i >= 0; i--)
                try { snapshot[i].Dispose(); }
                catch (Exception ex) { diagnostics.Record(CoreSeverity.Error, modId, "scope", "Scope release failed.", ex); }
        }
    }
}
