using Assets.Source.Items.Data;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class GameEventIntegration
    {
        private static void VitalsChanged(CombatantItemData target, string operation, float beforeHealth, float beforeEnergy)
        {
            if (target.Health01 == beforeHealth && target.Energy01 == beforeEnergy) return;
            Publish(new NativeCombatantVitalsChangedEvent(target, operation,
                beforeHealth, target.Health01, beforeEnergy, target.Energy01));
        }
        [HarmonyPatch(typeof(CombatantItemData), nameof(CombatantItemData.TakeDamage),
            new[] { typeof(float), typeof(object), typeof(bool) })]
        private static class NativeDamage
        {
            static void Prefix(CombatantItemData __instance, out (float health, float energy) __state)
                => __state = (__instance.Health01, __instance.Energy01);
            static void Postfix(CombatantItemData __instance, (float health, float energy) __state)
                => VitalsChanged(__instance, nameof(CombatantItemData.TakeDamage), __state.health, __state.energy);
        }
        [HarmonyPatch(typeof(CombatantItemData), nameof(CombatantItemData.Heal),
            new[] { typeof(float), typeof(object) })]
        private static class NativeHeal
        {
            static void Prefix(CombatantItemData __instance, out (float health, float energy) __state)
                => __state = (__instance.Health01, __instance.Energy01);
            static void Postfix(CombatantItemData __instance, (float health, float energy) __state)
                => VitalsChanged(__instance, nameof(CombatantItemData.Heal), __state.health, __state.energy);
        }
        [HarmonyPatch(typeof(CombatantItemData), nameof(CombatantItemData.LoseEnergy),
            new[] { typeof(float), typeof(object) })]
        private static class NativeEnergy
        {
            static void Prefix(CombatantItemData __instance, out (float health, float energy) __state)
                => __state = (__instance.Health01, __instance.Energy01);
            static void Postfix(CombatantItemData __instance, (float health, float energy) __state)
                => VitalsChanged(__instance, nameof(CombatantItemData.LoseEnergy), __state.health, __state.energy);
        }
    }
}
