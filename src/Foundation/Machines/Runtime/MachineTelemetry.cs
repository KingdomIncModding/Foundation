using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    /// <summary>Observed actual native transfer notifications. Never estimates theoretical throughput.</summary>
    public sealed class MachineTelemetry
    {
        private sealed class Sample
        {
            public long Total;
            public readonly Queue<float> Recent = new Queue<float>();
        }

        private readonly Dictionary<FlowPipe, Sample> _samples = new Dictionary<FlowPipe, Sample>();
        private readonly List<Action<MachineTransferEvent>> _listeners = new List<Action<MachineTransferEvent>>();
        private sealed class CallbackToken : IDisposable
        {
            private readonly List<Action<MachineTransferEvent>> _listeners;
            private readonly Action<MachineTransferEvent> _action;
            private bool _disposed;
            public CallbackToken(List<Action<MachineTransferEvent>> listeners, Action<MachineTransferEvent> action)
            {
                _listeners = listeners;
                _action = action;
            }

            public void Dispose()
            {
                MachineThread.Verify();
                if (_disposed)
                    return;
                _disposed = true;
                _listeners.Remove(_action);
            }
        }

        public IDisposable ObserveTransfers(Action<MachineTransferEvent> observer)
        {
            MachineThread.Verify();
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            _listeners.Add(observer);
            return new CallbackToken(_listeners, observer);
        }

        internal void OnTransfer(FlowPipe pipe)
        {
            if (pipe == null || pipe.ConnectedInput == null || pipe.ConnectedOutput == null)
                return;
            if (!_samples.TryGetValue(pipe, out Sample sample))
            {
                sample = new Sample();
                _samples.Add(pipe, sample);
            }

            float now = Time.realtimeSinceStartup;
            sample.Total++;
            sample.Recent.Enqueue(now);
            Prune(sample, now);
            var evt = new MachineTransferEvent(pipe.ConnectedOutput.Parent.Guid, pipe.ConnectedInput.Parent.Guid, sample.Total, sample.Recent.Count / 5.0);
            foreach (var cb in _listeners.ToArray())
                try
                {
                    cb(evt);
                }
                catch (Exception ex)
                {
                    MachineRuntime.ReportCallbackError("Transfer subscriber", ex);
                }
        }

        private static void Prune(Sample sample, float now)
        {
            while (sample.Recent.Count > 0 && now - sample.Recent.Peek() > 5f)
                sample.Recent.Dequeue();
        }

        public bool TryRead(FlowPipe pipe, out long total, out double observedRate)
        {
            MachineThread.Verify();
            total = 0;
            observedRate = 0;
            if (pipe == null || !_samples.TryGetValue(pipe, out Sample sample))
                return false;
            Prune(sample, Time.realtimeSinceStartup);
            total = sample.Total;
            observedRate = sample.Recent.Count / 5.0;
            return true;
        }

        internal void ClearWorld()
        {
            _samples.Clear();
        }

        internal void Clear()
        {
            _samples.Clear();
            _listeners.Clear();
        }

        internal void PruneDeadPipes()
        {
            if (_samples.Count == 0)
                return;
            var living = new HashSet<FlowPipe>();
            var player = MachineThread.Player;
            if (player != null)
                foreach (var flow in player.Flows)
                    foreach (var pipe in flow.Pipes)
                        living.Add(pipe);
            foreach (var pipe in new List<FlowPipe>(_samples.Keys))
                if (!living.Contains(pipe))
                    _samples.Remove(pipe);
        }
    }
}
