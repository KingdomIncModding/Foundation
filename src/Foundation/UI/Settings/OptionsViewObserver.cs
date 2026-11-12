using System;
using KingdomFoundation.Api;
namespace KingdomFoundation
{
    internal sealed class OptionsViewObserver : IDisposable
    {
        readonly OptionsTabRegistry registry;
        internal Action<IOptionsView> Callback { get; private set; }
        internal OptionsViewObserver(OptionsTabRegistry registry, Action<IOptionsView> callback) { this.registry = registry; Callback = callback; }
        public void Dispose() { Callback = null; registry.RemoveObserver(this); }
    }
}
