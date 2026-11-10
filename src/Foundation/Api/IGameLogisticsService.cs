using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Native shared Armory and Portal stores, preserving channel semantics.</summary>
    public interface IGameLogisticsService
    {
        int ArmoryCount(int channel, ItemType type);
        InventoryItem PeekArmory(int channel, ItemType type);
        /// <summary>Invokes the armory types operation.</summary>
        /// <param name="channel">The channel argument.</param>
        /// <returns>The operation result.</returns>
        IReadOnlyList<ItemType> ArmoryTypes(int channel);
        void StoreInArmory(int channel, InventoryItem item);
        int PortalCount(int channel, int index);
        InventoryItem PeekPortal(int channel, int index);
        /// <summary>Attempts to store in portal.</summary>
        bool TryStoreInPortal(int channel, int index, InventoryItem item);
    }
}
