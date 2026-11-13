using System.Runtime.CompilerServices;
using Assets.Source.Adventure;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public sealed class GameWar
    {
        private static readonly ConditionalWeakTable<WarType,GameWar> Cache=new ConditionalWeakTable<WarType,GameWar>();
        internal WarType Native {get;}
        private GameWar(WarType native){Native=native;}
        internal static GameWar Wrap(WarType native)=>native==null ? null : Cache.GetValue(native,value=>new GameWar(value));
        public string Identifier=>Native.Identifier;
        public string DisplayName=>Native.DisplayName;
        public Sprite Icon=>Native.Icon;
    }
}
