using Assets.Source.Items.Data;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Real post-commit state change from CombatantItemData.TakeDamage, Heal, or
    /// LoseEnergy. Ratios are the native Health01 and Energy01 values, not an
    /// independent Foundation combat/health model.
    /// </summary>
    public sealed class NativeCombatantVitalsChangedEvent
    {
        public CombatantItemData Combatant { get; }
        public string NativeOperation { get; }
        public float PreviousHealth01 { get; }
        public float CurrentHealth01 { get; }
        public float PreviousEnergy01 { get; }
        public float CurrentEnergy01 { get; }
        public NativeCombatantVitalsChangedEvent(CombatantItemData combatant, string operation,
            float beforeHealth, float afterHealth, float beforeEnergy, float afterEnergy)
        {
            Combatant = combatant;
            NativeOperation = operation;
            PreviousHealth01 = beforeHealth;
            CurrentHealth01 = afterHealth;
            PreviousEnergy01 = beforeEnergy;
            CurrentEnergy01 = afterEnergy;
        }
    }
}
