using Assets.Source.Items;
using Assets.Source.Items.Data;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(HumanItemData), nameof(HumanItemData.AddEquipment), new[] { typeof(InventoryItem) })]
    internal static class NativeEquipmentAdded
    {
        static void Prefix(HumanItemData __instance, out InventoryItem[] __state) => __state = __instance.Equipment.ToArray();
        static void Postfix(HumanItemData __instance, InventoryItem[] __state)
        {
            // Native AddEquipment silently removes conflicting equipment.
            // Observe BOTH sides of that real operation rather than
            // reporting only the newly-equipped item.
            foreach (var removed in __state.Where(x => !__instance.Equipment.Contains(x)))
                global::KingdomFoundation.GameEventIntegration.Publish(new NativeEquipmentChangedEvent(__instance, removed, false));
            foreach (var added in __instance.Equipment.Where(x => !__state.Contains(x)))
                global::KingdomFoundation.GameEventIntegration.Publish(new NativeEquipmentChangedEvent(__instance, added, true));
        }
    }
}
