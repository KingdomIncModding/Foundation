using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Machines
{
    /// <summary>Owned subscriptions and native-backed extensions; dispose in consumer OnDestroy.</summary>
    public sealed class MachineModContext : IDisposable
    {
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        public string ModGuid { get; }

        private bool _disposed;
        internal MachineModContext(string guid)
        {
            MachineThread.Verify();
            if (string.IsNullOrWhiteSpace(guid))
                throw new ArgumentException("Mod GUID required", nameof(guid));
            ModGuid = guid;
        }

        private IDisposable Track(IDisposable item)
        {
            if (_disposed)
            {
                item.Dispose();
                throw new ObjectDisposedException(nameof(MachineModContext));
            }

            _registrations.Add(item);
            return item;
        }

        public IDisposable Listen(Action<MachineChangeEvent> listener) => Track(FoundationMachineApi.Events.Subscribe(listener));
        public IDisposable ListenTransfers(Action<MachineTransferEvent> listener) => Track(FoundationMachineApi.Telemetry.ObserveTransfers(listener));
        public IDisposable Expose(MachinePropertyDefinition property) => Track(FoundationMachineApi.Extensions.Register(ModGuid, property));
        public IDisposable RegisterCommand(MachineCommandDefinition command) => Track(FoundationMachineApi.Commands.Register(ModGuid, command));
        public MachineOperationResult ExecuteCommand(string localId, FlowElement native, object argument) => FoundationMachineApi.Commands.Execute(ModGuid + ":" + localId, native, argument);
        public MachineOperationResult Write(string propertyId, FlowElement machine, object value) => FoundationMachineApi.Extensions.TryWrite(ModGuid, propertyId, machine, value);
        public void Dispose()
        {
            MachineThread.Verify();
            if (_disposed)
                return;
            _disposed = true;
            for (int i = _registrations.Count - 1; i >= 0; i--)
                _registrations[i].Dispose();
            _registrations.Clear();
        }
    }
}
