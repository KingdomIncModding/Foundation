using System;
using System.Globalization;
using Assets.Source.Items;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>
    /// The base game uses colon both as a separator in item IDs and as a quality/seed
    /// suffix delimiter. Its FromTypeString assumes the first colon ends the item ID.
    /// Foundation recognizes ONLY previously registered namespaced item IDs and
    /// reconstructs them without changing the parser used for vanilla items.
    /// </summary>
    internal static partial class ItemIntegration
    {
        internal static void Install(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(ReadNamespacedItem)).Patch();
        }

    }
}
