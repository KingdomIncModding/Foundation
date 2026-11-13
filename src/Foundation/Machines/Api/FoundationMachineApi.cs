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
        public static bool IsKingdomLoaded
        {
            get
            {
                MachineThread.Verify();
                return MachineThread.Player != null;
            }
        }
    }
}
