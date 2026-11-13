using System;
using System.Globalization;
using Assets.Source.Items;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.FromTypeString))]
    internal static class ReadNamespacedItem
    {
        static bool Prefix(string itemStr, ref InventoryItem __result)
        {
            if (string.IsNullOrEmpty(itemStr) || itemStr.IndexOf(':') < 0)
                return true;
            string id = null;
            foreach (var known in FoundationApi.ContentInternal.NamespacedItemIds)
                if ((id == null || known.Length > id.Length) && (itemStr == known || itemStr.StartsWith(known + ":", StringComparison.Ordinal)))
                    id = known;
            if (id == null)
                return true;
            try
            {
                var type = ItemType.Get(id);
                var item = new InventoryItem(type);
                var suffix = itemStr.Substring(id.Length);
                if (suffix.Length > 0)
                {
                    foreach (var chunk in suffix.Substring(1).Split(':'))
                    {
                        if (chunk.Length < 2)
                            throw new FormatException("Malformed item quality/seed suffix for " + id);
                        switch (chunk[0])
                        {
                            case 'q':
                                item.SetQuality(int.Parse(chunk.Substring(1), CultureInfo.InvariantCulture) / 1000f);
                                break;
                            case 's':
                                AccessTools.Field(typeof(InventoryItem), "Seed").SetValue(item, chunk.Substring(1));
                                break;
                            default:
                                throw new FormatException("Unknown item suffix for " + id);
                        }
                    }
                }

                __result = item;
                return false;
            }
            catch (Exception error)
            {
                FoundationApi.Report(new InvalidOperationException("Could not restore namespaced item " + id, error));
                // Let the game's native parser fail normally on malformed data; never
                // silently substitute another item and corrupt a save.
                return true;
            }
        }
    }
}
