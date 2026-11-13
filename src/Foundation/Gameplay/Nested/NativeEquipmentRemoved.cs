using Assets.Source.Items;
using Assets.Source.Items.Data;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(HumanItemData), nameof(HumanItemData.RemoveEquipment), new[] { typeof(EquipmentSlot) })]
    internal static class NativeEquipmentRemoved
    {
        static void Prefix(HumanItemData __instance, EquipmentSlot slot, out InventoryItem __state) => __state = __instance.GetEquippedItem(slot);
        static void Postfix(HumanItemData __instance, InventoryItem __state)
        {
            if (__state != null && !__instance.Equipment.Contains(__state))
                global::KingdomFoundation.GameEventIntegration.Publish(new NativeEquipmentChangedEvent(__instance, __state, false));
        }
    }
}
