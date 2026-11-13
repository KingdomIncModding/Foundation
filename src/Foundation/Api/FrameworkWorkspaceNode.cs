using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using KingdomFoundation.Json;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Mod-owned workspace behavior hosted by a private native Foundation model.</summary>
    public abstract class FrameworkWorkspaceNode
    {
        internal HostedWorkspaceNode Native {get;}
        protected FrameworkWorkspaceNode(string typeId){Native=new HostedWorkspaceNode(this,typeId);}
        public WorkspaceObject Model=>GameWorkspaceAccess.IdentityFor(Native);
        public IReadOnlyList<WorkspacePort> Inputs=>Model.Inputs;
        public IReadOnlyList<WorkspacePort> Outputs=>Model.Outputs;
        public IReadOnlyList<WorkspacePort> IO=>Model.IO;
        public Component World=>Native.World;
        public Vector2Int Size {get=>Native.Size;set=>Native.Size=value;}
        public string Guid=>Native.Guid;
        public string ElementType=>Native.ElementType;
        public string Label=>Native.Label;
        public Vector2Int Position {get=>Native.Position;set=>Native.Position=value;}
        public IReadOnlyList<GameCurrency> CostCurrencies
        {
            get=>Native.CostCurrency.Select(value=>(GameCurrency)Enum.Parse(typeof(GameCurrency),value.ToString())).ToArray();
            set=>Native.CostCurrency=value.Select(currency=>GameCurrencyMap.Resolve(currency.ToString())).ToArray();
        }
        public WorkspacePort AddInput(int bufferSize,int position,Func<GameInventoryItem,bool> filter=null)
        {
            var input=new FlowInput(Native,Native.Inputs.Count){BufferSize=bufferSize,Position=position};
            if(filter!=null)input.Filter=item=>filter(GameInventoryItem.Wrap(item));Native.Inputs.Add(input);return WorkspacePort.Wrap(input);
        }
        public WorkspacePort AddOutput(int bufferSize,int position)
        {var output=new FlowOutput(Native,Native.Outputs.Count){BufferSize=bufferSize,Position=position};Native.Outputs.Add(output);return WorkspacePort.Wrap(output);}
        public void TrimInputs(int count)=>Native.Inputs.RemoveRange(count,Native.Inputs.Count-count);
        public void TrimOutputs(int count)=>Native.Outputs.RemoveRange(count,Native.Outputs.Count-count);
        public virtual void Update(float delta){}
        public virtual bool HandleReopenBuyMenu()=>false;
        protected virtual void DataToJson(JsonObject data){}
        protected virtual void DataFromJson(JsonObject data,GameWorkspace parent){}
        protected virtual void CopySettingsTo(FrameworkWorkspaceNode copy){}
        internal void SaveData(JsonObject data)=>DataToJson(data);
        internal void LoadData(JsonObject data,GameWorkspace parent)=>DataFromJson(data,parent);
        internal void CopyData(FrameworkWorkspaceNode copy)=>CopySettingsTo(copy);
        public static implicit operator WorkspaceObject(FrameworkWorkspaceNode node)=>node?.Model;
    }
}
