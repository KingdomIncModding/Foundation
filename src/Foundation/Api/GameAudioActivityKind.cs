namespace KingdomFoundation.Api
{
    /// <summary>Native operation provenance; consumers decide their own audio-channel policy.</summary>
    public enum GameAudioActivityKind
    {
        ManualCraft,
        MachineUpdate,
        EconomyAction,
        CombatAction
    }
}
