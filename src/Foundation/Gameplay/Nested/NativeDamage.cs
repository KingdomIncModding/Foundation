using Assets.Source.Items.Data;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(CombatantItemData), nameof(CombatantItemData.TakeDamage), new[] { typeof(float), typeof(object), typeof(bool) })]
    internal static class NativeDamage
    {
        static void Prefix(CombatantItemData __instance, out (float health, float energy) __state) => __state = (__instance.Health01, __instance.Energy01);
        static void Postfix(CombatantItemData __instance, (float health, float energy) __state) => NativeCombatantVitals.VitalsChanged(__instance, nameof(CombatantItemData.TakeDamage), __state.health, __state.energy);
    }
}
