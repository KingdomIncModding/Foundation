namespace FoundationExamples.CoreMod
{
    internal sealed class ExampleCounter : IExampleCounter
    {
        public int Count { get; private set; }
        public void Increment() => Count++;
    }
}
