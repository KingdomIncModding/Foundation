using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Source.Items;

namespace KingdomFoundation.Api
{
    public sealed class GameCraftingInput
    {
        private static readonly ConditionalWeakTable<CraftingInput,GameCraftingInput> Cache=new ConditionalWeakTable<CraftingInput,GameCraftingInput>();
        internal CraftingInput Native {get;}
        private GameCraftingInput(CraftingInput native){Native=native;}
        internal static GameCraftingInput Wrap(CraftingInput native)=>native==null ? null : Cache.GetValue(native,value=>new GameCraftingInput(value));
        public IEnumerable<GameInventoryItem> AllItems=>Native.AllItems.Select(GameInventoryItem.Wrap);
    }
}
