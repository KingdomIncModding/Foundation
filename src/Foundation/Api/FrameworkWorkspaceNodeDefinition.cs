using System;
using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Registration metadata for mod-owned behavior, without native game types in the consumer contract.</summary>
    public sealed class FrameworkWorkspaceNodeDefinition
    {
        internal WorkspaceNodeDefinition NativeDefinition {get;}
        public string Id=>NativeDefinition.Id;
        public FrameworkWorkspaceNodeDefinition(string id,string title,Func<FrameworkWorkspaceNode> factory,string requiredTechId,
            Vector2Int size=default,string description="",string category="Utility",string iconName="Icons_104",Sprite icon=null,
            double costMultiplier=1,double goldMultiplier=1,GameMoney fixedCost=null)
        {
            if(factory==null)throw new ArgumentNullException(nameof(factory));
            if(!Enum.TryParse<ElementCategory>(category,out var nativeCategory))throw new ArgumentException("Unknown workspace category.",nameof(category));
            NativeDefinition=new WorkspaceNodeDefinition(id,title,()=>factory().Native,requiredTechId,size,description,nativeCategory,iconName,icon,costMultiplier,goldMultiplier,fixedCost?.Native);
        }
    }
}
