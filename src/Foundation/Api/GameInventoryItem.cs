using System;
using System.Runtime.CompilerServices;
using Assets.Source.Items;

namespace KingdomFoundation.Api
{
    public sealed class GameInventoryItem
    {
        private static readonly ConditionalWeakTable<InventoryItem,GameInventoryItem> Cache=new ConditionalWeakTable<InventoryItem,GameInventoryItem>();
        internal InventoryItem Native {get;}
        private GameInventoryItem(InventoryItem native){Native=native;}
        internal static GameInventoryItem Wrap(InventoryItem native)=>native==null ? null : Cache.GetValue(native,value=>new GameInventoryItem(value));
        public GameInventoryItem(GameItemType type)
        {if(type==null)throw new ArgumentNullException(nameof(type));Native=new InventoryItem(type.Native);Cache.Add(Native,this);}
        public long SellValue=>Native.SellValue;
        public string DisplayName=>Native.DisplayName;
        public UnityEngine.Sprite Icon=>Native.Icon;
        public GameItemType Type=>GameItemType.Wrap(Native.Type);
    }
}
