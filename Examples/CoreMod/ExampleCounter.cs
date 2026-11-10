namespace FoundationExamples.CoreMod
{
    /// <summary>Defines the example counter type.</summary>
    internal sealed class ExampleCounter : IExampleCounter
    {
        /// <summary>Gets or sets the count value.</summary>
        public int Count { get; private set; }
        public void Increment() => Count++;
    }
}
