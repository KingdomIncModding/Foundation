namespace FoundationExamples.CoreMod
{
    /// <summary>Provides the iexample counter API.</summary>
    public interface IExampleCounter
    {
        int Count { get; }
        void Increment();
    }
}
