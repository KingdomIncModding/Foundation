using System;
namespace KingdomFoundation.Machines
{
    /// <summary>Native persisted FlowElement.Guid. Unique within a kingdom, not across unrelated saves.</summary>
    public sealed class MachineIdentity : IEquatable<MachineIdentity>
    {
        public string Guid { get; }
        public int FlowIndex { get; }
        public MachineIdentity(string guid, int flowIndex)
        {
            if (string.IsNullOrWhiteSpace(guid)) throw new ArgumentException("Native GUID required", nameof(guid));
            Guid = guid;
            FlowIndex = flowIndex;
        }
        /// <summary>Performs the equals operation.</summary>
        public bool Equals(MachineIdentity other) => other != null && Guid == other.Guid && FlowIndex == other.FlowIndex;
        public override bool Equals(object obj) => Equals(obj as MachineIdentity);
        public override int GetHashCode() { unchecked { return (Guid.GetHashCode() * 397) ^ FlowIndex; } }
        public override string ToString() => $"{FlowIndex}:{Guid}";
    }
}
