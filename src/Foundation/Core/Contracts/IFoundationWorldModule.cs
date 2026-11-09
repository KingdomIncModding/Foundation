using Assets.Source.Player;

namespace KingdomFoundation.Core
{
    public interface IFoundationWorldModule
    {
        void WorldLoaded(GamePlayer world);
        void WorldUnloading(GamePlayer world);
    }
}
