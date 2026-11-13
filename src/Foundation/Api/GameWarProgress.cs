using System.Runtime.CompilerServices;
using Assets.Source.Adventure;

namespace KingdomFoundation.Api
{
    public sealed class GameWarProgress
    {
        private static readonly ConditionalWeakTable<WarProgress,GameWarProgress> Cache=new ConditionalWeakTable<WarProgress,GameWarProgress>();
        internal WarProgress Native {get;}
        private GameWarProgress(WarProgress native){Native=native;}
        internal static GameWarProgress Wrap(WarProgress native)=>native==null ? null : Cache.GetValue(native,value=>new GameWarProgress(value));
        public GameWar War=>GameWar.Wrap(Native.War);
    }
}
