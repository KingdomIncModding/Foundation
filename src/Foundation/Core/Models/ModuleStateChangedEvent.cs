namespace KingdomFoundation.Core
{
    public sealed class ModuleStateChangedEvent
    {
        public ModSnapshot Module { get; }
        public ModuleStateChangedEvent(ModSnapshot snapshot) { Module = snapshot; }
    }
}
