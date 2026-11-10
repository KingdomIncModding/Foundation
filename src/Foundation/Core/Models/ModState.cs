namespace KingdomFoundation.Core
{
    /// <summary>Provides the mod state API.</summary>
    public enum ModState
    {
        Registered,
        WaitingForDependencies,
        Starting,
        Active,
        Failed,
        Stopping,
        Stopped,
        Disposed
    }
}
