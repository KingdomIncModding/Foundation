namespace KingdomFoundation.Api
{
    /// <summary>Presentation of a Game-tab section heading; hiding it leaves its settings available.</summary>
    public interface IVanillaSectionView
    {
        string Id { get; }
        string Title { get; }
        bool IsAlive { get; }
        /// <summary>Heading visibility. An empty section stays hidden even when visibility is requested.</summary>
        bool IsVisible { get; set; }
    }
}
