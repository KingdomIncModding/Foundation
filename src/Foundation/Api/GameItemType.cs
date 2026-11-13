using System.Runtime.CompilerServices;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public sealed class GameItemType
    {
        private static readonly ConditionalWeakTable<ItemType,GameItemType> Cache=new ConditionalWeakTable<ItemType,GameItemType>();
        internal ItemType Native { get; }
        private GameItemType(ItemType native) {Native=native;}
        internal static GameItemType Wrap(ItemType native)=>native==null ? null : Cache.GetValue(native,value=>new GameItemType(value));
        public string Identifier=>Native.Identifier;
        public int Ordinal=>Native.Ordinal;
        public GameInventoryItem PreviewItem=>GameInventoryItem.Wrap(Native.PreviewItem);
        public bool IsHuman=>Native is Assets.Source.Items.Type.HumanItemType;
        public string PreviewDisplayName=>Native is ResourceItemType resource ? resource.PreviewItem.DisplayName : Native is Assets.Source.Items.Type.ForestItemType forest ? forest.PreviewItem.DisplayName : Native.GetDisplayName();
        public string IconName=>Native.IconName;
        public bool IsUnlocked=>Native.IsUnlocked;
        public GameRecipe DefaultRecipe=>GameRecipe.Wrap(Native.DefaultRecipe);
        public bool HasSourceQuest=>Native.SourceQuest;
        public System.Collections.Generic.IReadOnlyList<GameResearchNode> SourceQuestResearch=>System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(Native.GetSourceQuests(),quest=>GameResearchNode.Wrap(quest.RequiredTech)));
        public string GetDisplayName(GameInventoryItem item=null)=>Native.GetDisplayName(item?.Native);
        public Sprite GetIcon(GameInventoryItem item=null)=>Native.GetIcon(item?.Native);
    }
}
