using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameLogisticsService : IGameLogisticsService
    {
        static GamePlayer Player => GameWorldService.RequirePlayer();
        static void ValidateArmory(int channel)
        {
            if (channel < 0 || channel >= ArmoryContents.ChannelCount) throw new ArgumentOutOfRangeException(nameof(channel));
        }
        static void ValidatePortal(int channel, int index)
        {
            if (channel < 0 || channel >= PortalContents.ChannelCount) throw new ArgumentOutOfRangeException(nameof(channel));
            if (index < 0 || index >= PortalContents.IndexCount) throw new ArgumentOutOfRangeException(nameof(index));
        }
        public int ArmoryCount(int channel, ItemType type)
        {
            ValidateArmory(channel); if (type == null) throw new ArgumentNullException(nameof(type));
            return Player.Armory.GetItemCount(channel, type);
        }
        public InventoryItem PeekArmory(int channel, ItemType type)
        {
            ValidateArmory(channel); if (type == null) throw new ArgumentNullException(nameof(type));
            return Player.Armory.PeekItem(channel, type);
        }
        public IReadOnlyList<ItemType> ArmoryTypes(int channel)
        {
            ValidateArmory(channel);
            return Array.AsReadOnly(Player.Armory.GetAvailableItemTypes(channel).ToArray());
        }
        public void StoreInArmory(int channel, InventoryItem item)
        {
            ValidateArmory(channel); if (item == null) throw new ArgumentNullException(nameof(item));
            Player.Armory.AddItem(channel, item);
        }
        public int PortalCount(int channel, int index)
        {
            ValidatePortal(channel, index);
            return Player.Portal.GetItemCount(channel, index);
        }
        public InventoryItem PeekPortal(int channel, int index)
        {
            ValidatePortal(channel, index);
            return Player.Portal.PeekItem(channel, index);
        }
        public bool TryStoreInPortal(int channel, int index, InventoryItem item)
        {
            ValidatePortal(channel, index);
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!Player.Portal.CanAddTo(channel, index)) return false;
            Player.Portal.AddItem(channel, index, item);
            return true;
        }
    }
}
