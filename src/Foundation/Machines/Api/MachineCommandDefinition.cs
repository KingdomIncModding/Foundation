using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Machines
{
    /// <summary>A consumer-owned action routed to existing game-native methods by the registering mod.</summary>
    public sealed class MachineCommandDefinition
    {
        public string Id { get; }
        public Type MachineType { get; }
        public Func<FlowElement, object, MachineOperationResult> InvokeNative { get; }

        public MachineCommandDefinition(string id, Type machineType, Func<FlowElement, object, MachineOperationResult> invokeNative)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Command ID required", nameof(id));
            if (machineType == null || !typeof(FlowElement).IsAssignableFrom(machineType))
                throw new ArgumentException("Native FlowElement-derived type required", nameof(machineType));
            Id = id;
            MachineType = machineType;
            InvokeNative = invokeNative ?? throw new ArgumentNullException(nameof(invokeNative));
        }
    }
}
