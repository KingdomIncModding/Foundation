using System;
using KingdomFoundation.Api;
namespace KingdomFoundation
{
    internal sealed class OptionsTabRegistration : IOptionsTabRegistration
    {
        readonly OptionsTabRegistry registry;
        internal readonly string ModId, Id;
        readonly IModConfigSection settings;
        public IOptionsTab Tab { get; }
        public bool IsRegistered { get; private set; } = true;
        internal OptionsTabRegistration(OptionsTabRegistry registry, IOptionsTab tab)
        { this.registry = registry; Tab = tab; ModId = tab.ModId; Id = tab.Id; settings = tab.Settings; }
        public void Refresh()
        {
            if (!IsRegistered) throw new ObjectDisposedException(nameof(IOptionsTabRegistration));
            OptionsTabRegistry.Validate(Tab);
            if (Tab.ModId != ModId || Tab.Id != Id) throw new InvalidOperationException("Tab identity cannot change.");
            if (!ReferenceEquals(Tab.Settings, settings)) throw new InvalidOperationException("A tab cannot replace its settings section; unregister it first.");
            registry.Notify();
        }
        public void Dispose() { if (!IsRegistered) return; IsRegistered = false; registry.Remove(this); }
    }
}
