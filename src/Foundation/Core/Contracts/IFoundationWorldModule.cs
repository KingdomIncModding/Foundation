using Assets.Source.Player;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the ifoundation world module API.</summary>
    public interface IFoundationWorldModule
    {
        void WorldLoaded(GamePlayer world);
        void WorldUnloading(GamePlayer world);
    }
}
