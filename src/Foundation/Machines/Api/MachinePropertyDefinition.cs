using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Machines
{
    /// <summary>Optional owner-scoped native-backed properties. This does NOT extend/override game simulation.</summary>
    public sealed class MachinePropertyDefinition
    {
        public string Id { get; }
        public Type MachineType { get; }
        public Func<FlowElement, object> Read { get; }
        public Action<FlowElement, object> Write { get; }

        public MachinePropertyDefinition(string id, Type machineType, Func<FlowElement, object> read, Action<FlowElement, object> write = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Property id required", nameof(id));
            if (machineType == null || !typeof(FlowElement).IsAssignableFrom(machineType))
                throw new ArgumentException("MachineType must extend native FlowElement", nameof(machineType));
            Id = id;
            MachineType = machineType;
            Read = read ?? throw new ArgumentNullException(nameof(read));
            Write = write;
        }
    }
}
