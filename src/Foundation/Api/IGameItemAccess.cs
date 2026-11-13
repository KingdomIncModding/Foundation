using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    public interface IGameItemAccess
    {
        System.Collections.Generic.IReadOnlyList<GameRecipe> Recipes {get;}
        System.Collections.Generic.IReadOnlyList<GameItemType> Unlocked {get;}
        IReadOnlyList<GameItemType> Items {get;}
        GameItemType Find(string id);
        bool CanSell(GameInventoryItem item);
    }
}
