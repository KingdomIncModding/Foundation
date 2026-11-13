namespace KingdomFoundation.Api
{
    public readonly struct GameAudioActivity
    {
        public GameAudioActivityKind Kind { get; }
        public bool ManualQueued { get; }
        internal GameAudioActivity(GameAudioActivityKind kind, bool manualQueued = false) { Kind = kind; ManualQueued = manualQueued; }
    }
}
