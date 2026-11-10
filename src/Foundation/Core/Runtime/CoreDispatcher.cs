using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KingdomFoundation.Core
{
    internal sealed class CoreDispatcher : ICoreDispatcher
    {
        readonly object gate = new object();
        readonly Queue<CoreDispatchJob> pending = new Queue<CoreDispatchJob>();
        readonly CoreDiagnostics diagnostics;
        int mainThread;
        bool stopped;
        internal CoreDispatcher(CoreDiagnostics diagnostics) { this.diagnostics = diagnostics; }
        internal void AttachMainThread() => mainThread = Thread.CurrentThread.ManagedThreadId;
        /// <summary>Gets or sets the is main thread value.</summary>
        public bool IsMainThread => mainThread != 0 && Thread.CurrentThread.ManagedThreadId == mainThread;
        public void RequireMainThread()
        {
            if (!IsMainThread) throw new InvalidOperationException("Native Kingdom Inc calls must execute on the Unity main thread.");
        }
        public IDisposable Post(string ownerId, Action callback)
        {
            ModIdentity.Validate(ownerId);
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            var item = new CoreDispatchJob(ownerId, callback);
            lock (gate)
            {
                if (stopped) throw new ObjectDisposedException(nameof(CoreDispatcher));
                pending.Enqueue(item);
            }
            return item;
        }
        internal void Drain(int maximum = 256)
        {
            RequireMainThread();
            for (int i = 0; i < maximum; i++)
            {
                CoreDispatchJob task;
                lock (gate)
                {
                    if (pending.Count == 0) return;
                    task = pending.Dequeue();
                }
                if (!task.TryRun()) continue;
                try { task.Callback(); }
                catch (Exception ex)
                {
                    diagnostics.Record(CoreSeverity.Error, task.OwnerId, "dispatcher", "Queued Unity action failed.", ex);
                }
            }
        }
        /// <summary>Performs the cancel owner operation.</summary>
        public void CancelOwner(string ownerId)
        {
            lock (gate)
                foreach (var job in pending.Where(x => x.OwnerId == ownerId)) job.Dispose();
        }
        internal void Stop()
        {
            lock (gate)
            {
                stopped = true;
                foreach (var job in pending) job.Dispose();
                pending.Clear();
            }
        }
    }
}
