using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Notification after a native machine changes its selected recipe.</summary>
    public sealed class MachineRecipeChangedEvent
    {
        public RecipeElement Machine { get; }
        public CraftingRecipe Previous { get; }
        public CraftingRecipe Current { get; }
        public MachineRecipeChangedEvent(RecipeElement machine, CraftingRecipe previous, CraftingRecipe current)
        { Machine = machine; Previous = previous; Current = current; }
    }
}
