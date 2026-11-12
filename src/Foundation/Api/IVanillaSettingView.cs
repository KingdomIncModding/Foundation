namespace KingdomFoundation.Api
{
    /// <summary>Presentation of one existing vanilla row, not its value or gameplay behavior. Valid for one UI lifetime.</summary>
    public interface IVanillaSettingView
    {
        string Id { get; }
        bool IsAlive { get; }
        bool IsVisible { get; set; }
    }
}
