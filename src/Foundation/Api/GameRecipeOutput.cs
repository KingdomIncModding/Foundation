namespace KingdomFoundation.Api
{
    /// <summary>Read-only recipe output descriptor.</summary>
    public sealed class GameRecipeOutput
    {
        internal GameRecipeOutput(GameItemType item, int count, float probability, bool hasOutput)
        {
            Item = item;
            Count = count;
            Probability = probability;
            HasOutput = hasOutput;
        }

        public GameItemType Item { get; }
        public int Count { get; }
        public float Probability { get; }
        public bool HasOutput { get; }
    }
}

