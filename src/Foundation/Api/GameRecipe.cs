using System.Runtime.CompilerServices;
using Assets.Source.Items;

namespace KingdomFoundation.Api
{
    public sealed class GameRecipe
    {
        private static readonly ConditionalWeakTable<CraftingRecipe,GameRecipe> Cache=new ConditionalWeakTable<CraftingRecipe,GameRecipe>();
        internal CraftingRecipe Native {get;}
        private GameRecipe(CraftingRecipe native){Native=native;}
        internal static GameRecipe Wrap(CraftingRecipe native)=>native==null ? null : Cache.GetValue(native,value=>new GameRecipe(value));
        public string Identifier=>Native.Identifier;
        public string DisplayName=>Native.DisplayName;
        public GameResearchNode RequiredResearch=>GameResearchNode.Wrap(Native.RequiredTech);
    }
}
