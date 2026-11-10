namespace KingdomFoundation.Core
{
    /// <summary>Provides the module state changed event API.</summary>
    public sealed class ModuleStateChangedEvent
    {
        public ModSnapshot Module { get; }
        public ModuleStateChangedEvent(ModSnapshot snapshot) { Module = snapshot; }
    }
}
