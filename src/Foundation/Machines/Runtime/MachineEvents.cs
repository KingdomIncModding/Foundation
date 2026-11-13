using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Player;

namespace KingdomFoundation.Machines
{
    /// <summary>Structural + configuration observation. Continuous progress is deliberately excluded.</summary>
    public sealed class MachineEvents
    {
        private sealed class View
        {
            public FlowElement Native;
            public int Flow;
            public string State;
            public string Layout;
        }

        private readonly Dictionary<string, View> _previous = new Dictionary<string, View>();
        private readonly List<Action<MachineChangeEvent>> _observers = new List<Action<MachineChangeEvent>>();
        private GamePlayer _lastPlayer;
        private bool _initialized;
        private sealed class Token : IDisposable
        {
            private readonly List<Action<MachineChangeEvent>> _list;
            private readonly Action<MachineChangeEvent> _observer;
            private bool _disposed;
            public Token(List<Action<MachineChangeEvent>> list, Action<MachineChangeEvent> observer)
            {
                _list = list;
                _observer = observer;
            }

            public void Dispose()
            {
                MachineThread.Verify();
                if (_disposed)
                    return;
                _disposed = true;
                _list.Remove(_observer);
            }
        }

        public IDisposable Subscribe(Action<MachineChangeEvent> observer)
        {
            MachineThread.Verify();
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            _observers.Add(observer);
            return new Token(_observers, observer);
        }

        private void Publish(MachineChangeKind type, string guid, View view)
        {
            if (view == null || view.Native == null)
                return;
            var evt = new MachineChangeEvent(type, new MachineIdentity(guid, view.Flow), view.Native.ElementType);
            foreach (var observer in _observers.ToArray())
                try
                {
                    observer(evt);
                }
                catch (Exception ex)
                {
                    MachineRuntime.ReportCallbackError("Machine event listener", ex);
                }
        }

        internal void Scan()
        {
            var player = MachineThread.Player;
            if (!ReferenceEquals(player, _lastPlayer))
            {
                _previous.Clear();
                _initialized = false;
                _lastPlayer = player;
                FoundationMachineApi.Telemetry.ClearWorld();
            }

            if (player == null)
                return;
            var current = new Dictionary<string, View>();
            for (int flowIndex = 0; flowIndex < player.Flows.Count; flowIndex++)
            {
                foreach (var el in player.Flows[flowIndex].Elements)
                {
                    if (el == null || string.IsNullOrEmpty(el.Guid))
                        continue;
                    if (current.ContainsKey(el.Guid))
                    {
                        MachineRuntime.LogWarning("Duplicate native GUID in active kingdom: " + el.Guid);
                        continue;
                    }

                    current.Add(el.Guid, new View { Native = el, Flow = flowIndex, State = Fingerprint(el), Layout = PortLayout(el) });
                }
            }

            if (_initialized)
            {
                foreach (var pair in _previous)
                    if (!current.ContainsKey(pair.Key))
                        Publish(MachineChangeKind.Removed, pair.Key, pair.Value);
                foreach (var pair in current)
                {
                    if (!_previous.TryGetValue(pair.Key, out View old))
                    {
                        Publish(MachineChangeKind.Added, pair.Key, pair.Value);
                        continue;
                    }

                    if (!ReferenceEquals(old.Native, pair.Value.Native))
                    {
                        Publish(MachineChangeKind.Removed, pair.Key, old);
                        Publish(MachineChangeKind.Added, pair.Key, pair.Value);
                        continue;
                    }

                    if (old.Flow != pair.Value.Flow)
                        Publish(MachineChangeKind.MovedFlow, pair.Key, pair.Value);
                    if (old.Layout != pair.Value.Layout)
                        Publish(MachineChangeKind.PortsChanged, pair.Key, pair.Value);
                    if (old.State != pair.Value.State)
                        Publish(MachineChangeKind.SettingsChanged, pair.Key, pair.Value);
                }
            }

            _previous.Clear();
            foreach (var p in current)
                _previous.Add(p.Key, p.Value);
            _initialized = true;
            FoundationMachineApi.Telemetry.PruneDeadPipes();
        }

        private static string PortLayout(FlowElement el)
        {
            // Only structural port layout. FlowElement is reference-identical across polls.
            var state = el.Inputs.Count + "|" + el.Outputs.Count;
            foreach (var port in el.Inputs)
                state += "|in:" + port.ParentIndex + ":" + port.ConnectedPipe?.ConnectedOutput?.Parent?.Guid + ":" + port.ConnectedPipe?.ConnectedOutput?.ParentIndex;
            foreach (var port in el.Outputs)
                state += "|out:" + port.ParentIndex + ":" + port.ConnectedPipe?.ConnectedInput?.Parent?.Guid + ":" + port.ConnectedPipe?.ConnectedInput?.ParentIndex;
            return state;
        }

        private static string Fingerprint(FlowElement el)
        {
            // Snapshot the actual properties whose mutation is controlled by game/UI.
            string v = el.Position.x + "," + el.Position.y + "|" + el.StackSize + "|" + el.Mirrored + "|" + el.Inputs.Count + "|" + el.Outputs.Count;
            if (el is Assets.Source.Flow.Elements.RecipeElement r)
                v += "|recipe:" + r.Recipe?.Identifier;
            if (el is Assets.Source.Flow.Elements.ResourceElement res)
                v += "|resource:" + res.Resource?.Identifier;
            if (el is Assets.Source.Flow.Elements.Forest forest)
                v += "|forest:" + forest.SelectedResource?.Identifier;
            if (el is Assets.Source.Flow.Elements.Filter filter)
                v += "|filter:" + filter.FilterType?.Identifier;
            if (el is Assets.Source.Flow.Elements.TrackSwitch sw)
                v += "|switch:" + sw.SwitchPosition;
            if (el is Assets.Source.Flow.Elements.Timer timer)
                v += "|timer:" + timer.MaxTime;
            if (el is Assets.Source.Flow.Elements.Refiner refiner)
                v += "|mana:" + refiner.UseMana;
            if (el is Assets.Source.Flow.Elements.Distributor distributor)
                v += "|out:" + distributor.OutputCount;
            if (el is Assets.Source.Flow.Elements.Collector collector)
                v += "|in:" + collector.InputCount;
            if (el is Assets.Source.Flow.Elements.Manifold manifold)
                v += "|in:" + manifold.InputCount;
            if (el is Assets.Source.Flow.Elements.Stockpile pile)
                v += "|capacity:" + pile.Capacity;
            if (el is Assets.Source.Flow.Elements.Armory armory)
                v += "|channel:" + armory.Channel + "|type:" + armory.OutputType?.Identifier;
            if (el is Assets.Source.Flow.Elements.Portal portal)
                v += "|portal:" + portal.Channel + ":" + portal.ChannelIndex + ":" + portal.IsOutput;
            if (el is Assets.Source.Flow.Elements.QuestBoard quest)
                v += "|quest:" + quest.SelectedQuest?.Quest?.Identifier;
            if (el is Assets.Source.Flow.Elements.Dungeon dung)
                v += "|dungeon:" + dung.CurrentDungeon?.Dungeon?.Identifier;
            if (el is Assets.Source.Flow.Elements.RequisitionOffice req)
                v += "|contract:" + req.SelectedContract;
            // Native settings whose changes were not previously captured.
            // These reflect player configuration, not transient production state.
            if (el is Assets.Source.Flow.Elements.Appraiser appraisal)
                v += "|appraisal:" + appraisal.StatAppraised + ":" + appraisal.AppraiseType + ":" + appraisal.StatAmount;
            if (el is Assets.Source.Flow.Elements.Accountant accountant)
                v += "|accountant:" + accountant.CurrType + ":" + accountant.AppraiseType + ":" + accountant.CurrAmount;
            if (el is Assets.Source.Flow.Elements.WarEffort war)
                v += "|war:" + war.WarType?.Identifier;
            if (el is Assets.Source.Flow.Elements.Outfitter outfitter)
                v += "|outfitterTimeout:" + outfitter.Timeout;
            return v;
        }

        internal void Clear()
        {
            _previous.Clear();
            _lastPlayer = null;
            _initialized = false;
            _observers.Clear();
        }
    }
}
