using System;

namespace KingdomFoundation.Core
{
    public sealed class ModSnapshot
    {
        public ModDescriptor Descriptor { get; }
        public ModState State { get; }
        public string Message { get; }
        public DateTime LastChangedUtc { get; }
        public ModSnapshot(ModDescriptor descriptor, ModState state, string message, DateTime lastChangedUtc)
        { Descriptor = descriptor; State = state; Message = message; LastChangedUtc = lastChangedUtc; }
    }
}
