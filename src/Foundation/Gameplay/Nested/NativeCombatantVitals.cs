using Assets.Source.Items.Data;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static class NativeCombatantVitals
    {
        internal static void VitalsChanged(CombatantItemData target, string operation, float beforeHealth, float beforeEnergy)
        {
            if (target.Health01 == beforeHealth && target.Energy01 == beforeEnergy)
                return;
            GameEventIntegration.Publish(new NativeCombatantVitalsChangedEvent(target, operation, beforeHealth, target.Health01, beforeEnergy, target.Energy01));
        }
    }
}
