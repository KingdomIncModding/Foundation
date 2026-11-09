using System;
using System.Collections.Generic;
using Assets.Source.Flow;
namespace KingdomFoundation.Machines
{
    /// <summary>Foundation Machines companion SDK. Vanilla simulation remains exclusively game-owned.</summary>
    public static class FoundationMachineApi
    {
        public static MachineRegistry Registry { get; } = new MachineRegistry();
        /// <summary>Source-grounded native class inventory and live-game prefab discovery.</summary>
        public static NativeMachineCatalog NativeTypes { get; } = new NativeMachineCatalog();
        /// <summary>Native FlowIO, pipe topology, measured transfers and logistic routing.</summary>
        public static MachineNativeIO IO { get; } = new MachineNativeIO();
        public static MachineOperations Operations { get; } = new MachineOperations();
        public static MachineProduction Production { get; } = new MachineProduction();
        public static MachineSpecializations Specializations { get; } = new MachineSpecializations();
        public static MachineCapabilities Capabilities { get; } = new MachineCapabilities();
        public static MachineTelemetry Telemetry { get; } = new MachineTelemetry();
        public static MachineEvents Events { get; } = new MachineEvents();
        public static MachineExtensions Extensions { get; } = new MachineExtensions();
        public static MachineCommands Commands { get; } = new MachineCommands();
        public static MachineModContext ForMod(string guid) => new MachineModContext(guid);
        /// <summary>Native active kingdom only; false at menu or during load.</summary>
        public static bool IsKingdomLoaded { get { MachineThread.Verify();return MachineThread.Player != null; } }
    }
    /// <summary>Owned subscriptions and native-backed extensions; dispose in consumer OnDestroy.</summary>
    public sealed class MachineModContext : IDisposable
    {
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        public string ModGuid { get; }
        private bool _disposed;
        internal MachineModContext(string guid)
        {
            MachineThread.Verify();
            if (string.IsNullOrWhiteSpace(guid)) throw new ArgumentException("Mod GUID required",nameof(guid));
            ModGuid=guid;
        }
        private IDisposable Track(IDisposable item)
        {
            if(_disposed) {item.Dispose();throw new ObjectDisposedException(nameof(MachineModContext));}
            _registrations.Add(item);return item;
        }
        public IDisposable Listen(Action<MachineChangeEvent> listener) => Track(FoundationMachineApi.Events.Subscribe(listener));
        public IDisposable ListenTransfers(Action<MachineTransferEvent> listener) => Track(FoundationMachineApi.Telemetry.ObserveTransfers(listener));
        public IDisposable Expose(MachinePropertyDefinition property) => Track(FoundationMachineApi.Extensions.Register(ModGuid,property));
        public IDisposable RegisterCommand(MachineCommandDefinition command) => Track(FoundationMachineApi.Commands.Register(ModGuid,command));
        public MachineOperationResult ExecuteCommand(string localId,FlowElement native,object argument) => FoundationMachineApi.Commands.Execute(ModGuid+":"+localId,native,argument);
        public MachineOperationResult Write(string propertyId,FlowElement machine,object value) => FoundationMachineApi.Extensions.TryWrite(ModGuid,propertyId,machine,value);
        public void Dispose()
        {
            MachineThread.Verify();
            if(_disposed) return;
            _disposed=true;
            for(int i=_registrations.Count-1;i>=0;i--) _registrations[i].Dispose();
            _registrations.Clear();
        }
    }
}
