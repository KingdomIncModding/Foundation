namespace KingdomFoundation.Api
{
    /// <summary>A mod-owned top-level Options page. Existing config elements supply its controls and edit lifecycle.</summary>
    public interface IOptionsTab
    {
        string ModId { get; }
        string Id { get; }
        string Title { get; }
        int Order { get; }
        IModConfigSection Settings { get; }
    }
}
