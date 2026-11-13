using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow.Elements;

namespace KingdomFoundation.Api
{
    internal sealed class GameItemAccess : IGameItemAccess
    {
        public IReadOnlyList<GameRecipe> Recipes=>Assets.Source.Items.CraftingRecipe.All.Select(GameRecipe.Wrap).ToArray();
        public IReadOnlyList<GameItemType> Unlocked=>Assets.Source.Items.ItemType.Unlocked.Select(GameItemType.Wrap).ToArray();
        public IReadOnlyList<GameItemType> Items=>Array.AsReadOnly(FoundationApi.Items.Items.Select(GameItemType.Wrap).ToArray());
        public GameItemType Find(string id)=>GameItemType.Wrap(FoundationApi.Items.FindItem(id));
        public bool CanSell(GameInventoryItem item)=>item!=null && Shop.CanSellItem(item.Native);
    }
}
