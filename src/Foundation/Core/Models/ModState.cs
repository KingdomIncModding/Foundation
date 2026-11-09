namespace KingdomFoundation.Core
{
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
