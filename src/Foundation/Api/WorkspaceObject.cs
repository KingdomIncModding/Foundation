using System;
using UnityEngine;
using Assets.Source.Flow;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Opaque native workspace identity with Foundation-owned descriptive values.</summary>
    public sealed class WorkspaceObject
    {
        internal object Native { get; }
        public FrameworkWorkspaceNode Behaviour=>(Native as HostedWorkspaceNode)?.Owner;
        internal WorkspaceObject(object native, WorkspaceObjectKind kind, string typeId, string name, Sprite icon, int stackSize, bool locked)
        { Native = native; Kind = kind; TypeId = typeId; Name = name; Icon = icon; StackSize = stackSize; Locked = locked; }
        public WorkspaceObjectKind Kind { get; private set; }
        public string TypeId { get; private set; }
        public string Name { get; private set; }
        public string Guid=>Native is FlowElement element ? element.Guid : null;
        public string IconName=>Native is FlowElement element ? element.IconName : (Native as FlowDeco)?.Sticker is Assets.Source.Flow.Stickers.IconSticker sticker ? sticker.IconName : null;
        public string StickerIconName=>(Native as FlowDeco)?.Sticker is Assets.Source.Flow.Stickers.IconSticker sticker ? sticker.IconName : null;
        public Sprite Icon { get; private set; }
        private int snapshotStackSize;
        public int StackSize { get => Native is FlowElement element ? Math.Max(1, element.StackSize) : snapshotStackSize; private set => snapshotStackSize = value; }
        public bool Stackable => Native is FlowElement element && element.Stackable;
        public bool IsContainer=>Native is FlowDeco deco && deco.IsContainer;
        public bool HasSticker=>Native is FlowDeco deco && deco.Sticker!=null;
        public Sprite StickerSprite=>(Native as FlowDeco)?.Sticker?.GetSprite();
        public GameWorkspace ParentWorkspace=>Native is FlowElement element ? GameWorkspace.Wrap(element.ParentFlow) : null;
        private GameProductionState production;
        public GameProductionState Production=>Native is FlowElement element ? production ?? (production=new GameProductionState(element)) : null;
        public bool IsSpawner=>Native is Assets.Source.Flow.Elements.DevSpawner;
        public GameItemType SpawnedItem
        {
            get=>Native is Assets.Source.Flow.Elements.DevSpawner spawner ? GameItemType.Wrap(spawner.Item) : null;
            set{if(Native is Assets.Source.Flow.Elements.DevSpawner spawner)spawner.Item=value?.Native;else throw new InvalidOperationException("Expected a native spawner identity.");}
        }
        public GameResearchNode RequiredResearch
        {
            get=>Native is FlowElement element ? GameResearchNode.Wrap(element.RequiredTech) : null;
            set{if(Native is FlowElement element)element.RequiredTech=value?.Native;else throw new InvalidOperationException("Expected a building identity.");}
        }
        public string Description
        {
            get=>Native is FlowElement element ? element.Description : "";
            set{if(Native is FlowElement element)element.Description=value;else throw new InvalidOperationException("Expected a building identity.");}
        }
        public string Label=>Native is FlowDeco deco ? deco.Label : ((FlowElement)Native).Label;
        public int StyleIndex
        {
            get=>Native is FlowDeco deco ? deco.StyleIndex : 0;
            set {if(Native is FlowDeco deco)deco.StyleIndex=value;else throw new InvalidOperationException("Only decorations have a style index.");}
        }
        public bool IsJunction => Native is Assets.Source.Flow.Elements.Collector || Native is Assets.Source.Flow.Elements.Distributor || Native is Assets.Source.Flow.Elements.Manifold;
        public bool Mirrored
        {
            get=>Native is FlowElement element && element.Mirrored;
            set { if (Native is FlowElement element) element.Mirrored=value; else throw new InvalidOperationException("Only buildings have mirrored ports."); }
        }
        public Component View => Native is FlowElement element ? element.World : (Native as FlowDeco)?.World;
        public Vector2Int Position => Native is FlowElement element ? element.Position : ((FlowDeco)Native).Position;
        public Vector2 Size => Native is FlowElement element ? (Vector2)element.Size : ((FlowDeco)Native).Size;
        public UnityEngine.Behaviour World => View as UnityEngine.Behaviour;
        private IReadOnlyList<WorkspacePort> inputs,outputs;
        public IReadOnlyList<WorkspacePort> Inputs => Native is FlowElement element ? inputs ?? (inputs=new WorkspacePortList(element,true)) : Array.Empty<WorkspacePort>();
        public IReadOnlyList<WorkspacePort> Outputs => Native is FlowElement element ? outputs ?? (outputs=new WorkspacePortList(element,false)) : Array.Empty<WorkspacePort>();
        public IReadOnlyList<WorkspacePort> IO => Native is FlowElement element ? Array.AsReadOnly(element.IO.Select(port => WorkspacePort.Wrap(port)).ToArray()) : Array.Empty<WorkspacePort>();
        public void SetPosition(Vector2Int position)
        { if(Native is FlowElement element)element.Position=position;else ((FlowDeco)Native).Position=position; }
        /// <summary>Change the model's stack count only. Update upgrades and view when the surrounding edit is ready.</summary>
        public void SetStackCount(int size) => ((FlowElement)Native).StackSize=size;
        public IReadOnlyList<GameOpaqueValue> Upgrades => ((FlowUpgrade[])UpgradeField.GetValue(Native))?.Select(value=>new GameOpaqueValue(value)).ToArray();
        private static readonly System.Reflection.FieldInfo UpgradeField=HarmonyLib.AccessTools.Field(typeof(FlowElement),"_upgrades");
        public void SetUpgrades(IEnumerable<GameOpaqueValue> values) => UpgradeField.SetValue(Native, values?.Select(value=>(FlowUpgrade)value.Native).ToArray());
        public void UpdateUpgrades() => ((FlowElement)Native).UpdateUpgrades();
        public void SetInputs(IEnumerable<WorkspacePort> ports)
        {var items=ports.Select(port=>(FlowInput)port.Native).ToArray();var element=(FlowElement)Native;element.Inputs.Clear();element.Inputs.AddRange(items);}
        public void SetOutputs(IEnumerable<WorkspacePort> ports)
        {var items=ports.Select(port=>(FlowOutput)port.Native).ToArray();var element=(FlowElement)Native;element.Outputs.Clear();element.Outputs.AddRange(items);}
        public void ClearViewReference() {if(Native is FlowElement element)element.World=null;else ((FlowDeco)Native).World=null;}
        /// <summary>Apply serialized native decoration properties while preserving its identity.</summary>
        public void ApplyDecorationData(KingdomFoundation.Json.JsonValue data)
        {
            var copy=FlowDeco.FromJson(data.Native,null);var deco=(FlowDeco)Native;
            deco.Position=copy.Position;deco.Size=copy.Size;deco.StyleIndex=copy.StyleIndex;deco.Label=copy.Label;
            deco.Locked=copy.Locked;deco.IsParent=copy.IsParent;deco.Sticker=copy.Sticker;
        }
        private bool snapshotLocked;
        public bool Locked { get=>Native is FlowDeco deco ? deco.Locked : snapshotLocked; set{if(Native is FlowDeco deco)deco.Locked=value;else snapshotLocked=value;} }
        internal void Refresh(WorkspaceObjectKind kind,string typeId,string name,Sprite icon,int stackSize,bool locked)
        { Kind=kind; TypeId=typeId; Name=name; Icon=icon; StackSize=stackSize; Locked=locked; }
        /// <summary>Identity comparison does not depend on native CLR type names.</summary>
        public override bool Equals(object obj) => obj is WorkspaceObject other && ReferenceEquals(Native, other.Native);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Native);
        public static bool operator ==(WorkspaceObject left, WorkspaceObject right) => ReferenceEquals(left,right) || (!ReferenceEquals(left,null) && left.Equals(right));
        public static bool operator !=(WorkspaceObject left, WorkspaceObject right) => !(left==right);
    }
}
