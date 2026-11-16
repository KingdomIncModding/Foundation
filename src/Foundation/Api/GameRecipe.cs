using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Source.Items;

namespace KingdomFoundation.Api
{
    public sealed class GameRecipe
    {
        private static readonly ConditionalWeakTable<CraftingRecipe,GameRecipe> Cache=new ConditionalWeakTable<CraftingRecipe,GameRecipe>();
        internal CraftingRecipe Native {get;}
        private GameRecipe(CraftingRecipe native){Native=native;}
        internal static GameRecipe Wrap(CraftingRecipe native)=>native==null ? null : Cache.GetValue(native,value=>new GameRecipe(value));
        public string Identifier=>Native.Identifier;
        public string DisplayName=>Native.DisplayName;
        public GameResearchNode RequiredResearch=>GameResearchNode.Wrap(Native.RequiredTech);
        public float CraftingTime=>Native.CraftingTime;
        public float BaseProductivity=>Native.BaseProductivity;
        public GameMoney CraftingCost=>Native.CraftingCost==null ? null : new GameMoney(Native.CraftingCost);
        public GameMoney CraftingReward=>Native.CraftingReward==null ? null : new GameMoney(Native.CraftingReward);
        public GameItemType PreviewResultItem=>GameItemType.Wrap(Native.PreviewResultItem);
        public IReadOnlyList<GameRecipeInput> Inputs
        {
            get
            {
                if(Native.Reagents==null||Native.Reagents.Length==0)return Array.Empty<GameRecipeInput>();
                var list=new GameRecipeInput[Native.Reagents.Length];
                for(int i=0;i<Native.Reagents.Length;i++)
                {
                    var r=Native.Reagents[i];
                    list[i]=new GameRecipeInput(GameItemType.Wrap(r.Item),r.Count,r.PreviewFilter);
                }
                return Array.AsReadOnly(list);
            }
        }
        public IReadOnlyList<GameRecipeOutput> Outputs
        {
            get
            {
                if(Native.Results==null||Native.Results.Length==0)return Array.Empty<GameRecipeOutput>();
                float totalWeight=0f;
                for(int i=0;i<Native.Results.Length;i++)
                    if(Native.Results[i].Probability>0f)totalWeight+=Native.Results[i].Probability;
                var list=new GameRecipeOutput[Native.Results.Length];
                for(int i=0;i<Native.Results.Length;i++)
                {
                    var r=Native.Results[i];
                    float prob=totalWeight>0f ? (r.Probability>0f ? r.Probability/totalWeight : 0f) : 1f;
                    list[i]=new GameRecipeOutput(GameItemType.Wrap(r.Item),r.Count,prob,r.HasOutput);
                }
                return Array.AsReadOnly(list);
            }
        }
    }
}
