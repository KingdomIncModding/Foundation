using System;

namespace KingdomFoundation.Core
{
    internal sealed class ModRegistration : IModRegistration
    {
        readonly CoreModuleRegistry registry;
        internal IFoundationModule Module { get; }
        readonly ModDescriptor descriptor;
        internal ModContext Context;
        internal ModScope Scope;
        internal long ActivationSequence;
        internal bool HasWorld;
        internal ModState CurrentState;
        internal string CurrentMessage;
        internal DateTime LastChanged;
        public ModDescriptor Descriptor => descriptor;
        public ModState State => CurrentState;
        public string Message => CurrentMessage;
        internal ModRegistration(CoreModuleRegistry registry, IFoundationModule module, ModDescriptor descriptor)
        {
            this.registry = registry;
            Module = module;
            this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            SetState(ModState.Registered, "Registered; awaiting activation.");
        }
        internal void SetState(ModState state, string message)
        {
            if (state == CurrentState && message == CurrentMessage) return;
            CurrentState = state;
            CurrentMessage = message;
            LastChanged = DateTime.UtcNow;
        }
        internal ModSnapshot Snapshot() => new ModSnapshot(Descriptor, State, Message, LastChanged);
        public void Dispose() => registry.Unregister(this);
    }
}
