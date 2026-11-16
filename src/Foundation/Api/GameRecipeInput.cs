namespace KingdomFoundation.Api
{
    /// <summary>Read-only recipe reagent descriptor.</summary>
    public sealed class GameRecipeInput
    {
        internal GameRecipeInput(GameItemType item, int count, bool filtered)
        {
            Item = item;
            Count = count;
            Filtered = filtered;
        }

        public GameItemType Item { get; }
        public int Count { get; }
        public bool Filtered { get; }
    }
}

