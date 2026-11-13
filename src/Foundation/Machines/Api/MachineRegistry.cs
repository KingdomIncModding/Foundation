using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Player;
namespace KingdomFoundation.Machines
{
    /// <summary>Live native kingdom catalog. Does not cache or clone game elements.</summary>
    public sealed class MachineRegistry
    {
        public IReadOnlyList<FlowElement> All()
        {
            MachineThread.Verify();
            var list = new List<FlowElement>();
            var player = MachineThread.Player;
            if (player == null) return list.AsReadOnly();
            foreach (var flow in player.Flows) list.AddRange(flow.Elements);
            return list.AsReadOnly();
        }
        public IReadOnlyList<FlowElement> OnFlow(int flowIndex)
        {
            MachineThread.Verify();
            var player = MachineThread.Player;
            if (player == null || flowIndex < 0 || flowIndex >= player.Flows.Count) return new List<FlowElement>().AsReadOnly();
            return new List<FlowElement>(player.Flows[flowIndex].Elements).AsReadOnly();
        }
        public IReadOnlyList<T> OfType<T>() where T : FlowElement
        {
            MachineThread.Verify();
            var result = new List<T>();
            foreach (var el in All()) if (el is T machine) result.Add(machine);
            return result.AsReadOnly();
        }
        public IReadOnlyList<FlowElement> Where(Func<FlowElement,bool> predicate)
        {
            MachineThread.Verify();
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var result = new List<FlowElement>();
            foreach (var el in All()) if (predicate(el)) result.Add(el);
            return result.AsReadOnly();
        }
        public FlowElement FindByGuid(string guid)
        {
            MachineThread.Verify();
            if (string.IsNullOrWhiteSpace(guid)) return null;
            foreach (var el in All()) if (el.Guid == guid) return el;
            return null;
        }
        public FlowElement Find(MachineIdentity identity)
        {
            MachineThread.Verify();
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            foreach (var el in OnFlow(identity.FlowIndex)) if (el.Guid == identity.Guid) return el;
            return null;
        }
        public MachineSnapshot Inspect(FlowElement el)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(el)) throw new InvalidOperationException("Machine is not part of the current native kingdom.");
            var player = MachineThread.Player;
            for (int i = 0; i < player.Flows.Count; i++)
                if (player.Flows[i].Elements.Contains(el)) return new MachineSnapshot(el, i, FoundationMachineApi.Extensions.ReadProperties(el), FoundationMachineApi.Specializations.Read(el));
            throw new InvalidOperationException("Machine was removed during inspection.");
        }
        public IReadOnlyList<MachineSnapshot> SnapshotAll()
        {
            MachineThread.Verify();
            var list = new List<MachineSnapshot>();
            var player = MachineThread.Player;
            if (player == null) return list.AsReadOnly();
            for (int i = 0; i < player.Flows.Count; i++)
                foreach (var el in player.Flows[i].Elements)
                    list.Add(new MachineSnapshot(el, i, FoundationMachineApi.Extensions.ReadProperties(el), FoundationMachineApi.Specializations.Read(el)));
            return list.AsReadOnly();
        }
        public IReadOnlyList<MachineConnection> Connections(int flowIndex)
        {
            MachineThread.Verify();
            var list = new List<MachineConnection>();
            var player = MachineThread.Player;
            if (player == null || flowIndex < 0 || flowIndex >= player.Flows.Count) return list.AsReadOnly();
            foreach (var pipe in player.Flows[flowIndex].Pipes)
            {
                if (pipe.ConnectedInput == null || pipe.ConnectedOutput == null) continue;
                FoundationMachineApi.Telemetry.TryRead(pipe, out long count, out double rate);
                list.Add(new MachineConnection(pipe, count, rate));
            }
            return list.AsReadOnly();
        }
        public IReadOnlyList<MachineConnection> ConnectionsOf(FlowElement machine)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) return new List<MachineConnection>().AsReadOnly();
            var list = new List<MachineConnection>();
            var player = MachineThread.Player;
            for (int i = 0; i < player.Flows.Count; i++)
                foreach (var pipe in player.Flows[i].Pipes)
                    if (pipe.ConnectedInput?.Parent == machine || pipe.ConnectedOutput?.Parent == machine)
                    {
                        FoundationMachineApi.Telemetry.TryRead(pipe, out long count, out double rate);
                        list.Add(new MachineConnection(pipe, count, rate));
                    }
            return list.AsReadOnly();
        }
    }
}
