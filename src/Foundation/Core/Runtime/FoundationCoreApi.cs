namespace KingdomFoundation.Core
{
    /// <summary>Stable entry point; available even before the companion BepInEx component starts.</summary>
    public static class FoundationCoreApi
    {
        private static readonly FoundationCore Instance = new FoundationCore();
        public static IFoundationCore Core => Instance;
        internal static FoundationCore Runtime => Instance;
    }
}
