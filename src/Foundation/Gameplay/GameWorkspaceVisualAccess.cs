using System;
using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    internal sealed class GameWorkspaceVisualAccess : IGameWorkspaceVisualAccess
    {
        private static readonly IReadOnlyDictionary<GameBuildingGraphic,System.Reflection.FieldInfo> BuildingGraphics=new Dictionary<GameBuildingGraphic,System.Reflection.FieldInfo>
        {
            {GameBuildingGraphic.Icon,AccessTools.Field(typeof(WorldFlowElement),"_icon")},
            {GameBuildingGraphic.Progress,AccessTools.Field(typeof(WorldFlowElement),"_progress")},
            {GameBuildingGraphic.Productivity,AccessTools.Field(typeof(WorldFlowElement),"_productivity")},
            {GameBuildingGraphic.OutputFull,AccessTools.Field(typeof(WorldFlowElement),"_outputFull")},
            {GameBuildingGraphic.CraftButton,AccessTools.Field(typeof(WorldFlowElement),"_craftButton")},
            {GameBuildingGraphic.UpgradeSlots,AccessTools.Field(typeof(WorldFlowElement),"_upgradeSlots")},
            {GameBuildingGraphic.Label,AccessTools.Field(typeof(WorldFlowElement),"_label")}
        };
        public Component BuildingGraphic(Component buildingView,GameBuildingGraphic graphic)=>BuildingGraphics[graphic].GetValue(Require<WorldFlowElement>(buildingView)) as Component;
        public void UpdateCollider(Component buildingView){var collider=Require<WorldFlowElement>(buildingView).Collider;if(collider)collider.UpdateSize();}
        private static readonly AccessTools.FieldRef<WorldFlowElement,RectTransform> PortParent=AccessTools.FieldRefAccess<WorldFlowElement,RectTransform>("_ioParent");
        private static readonly AccessTools.FieldRef<WorldFlowElement,WorldFlowIO> PortPrefab=AccessTools.FieldRefAccess<WorldFlowElement,WorldFlowIO>("_ioPrefab");
        private static readonly AccessTools.FieldRef<WorldFlowElement,Image> Label=AccessTools.FieldRefAccess<WorldFlowElement,Image>("_labelContainer");
        private static readonly AccessTools.FieldRef<WorldFlowElement,TMP_Text> Caption=AccessTools.FieldRefAccess<WorldFlowElement,TMP_Text>("_label");
        private static readonly AccessTools.FieldRef<WorldFlowElement,Image> IconField=AccessTools.FieldRefAccess<WorldFlowElement,Image>("_icon");
        private static readonly AccessTools.FieldRef<WorldFlowGhost,Image> PreviewLabel=AccessTools.FieldRefAccess<WorldFlowGhost,Image>("_labelRect");
        private static readonly AccessTools.FieldRef<WorldFlowElement,List<WorldFlowIO>> Ports=AccessTools.FieldRefAccess<WorldFlowElement,List<WorldFlowIO>>("_io");
        private static readonly AccessTools.FieldRef<WorldFlowIO,Image> SpriteGraphic=AccessTools.FieldRefAccess<WorldFlowIO,Image>("_sprite");
        private static readonly AccessTools.FieldRef<WorldFlowIO,Image> HintGraphic=AccessTools.FieldRefAccess<WorldFlowIO,Image>("_itemHint");
        private static readonly AccessTools.FieldRef<WorldFlowIO,TMP_Text> CountGraphic=AccessTools.FieldRefAccess<WorldFlowIO,TMP_Text>("_itemCount");
        private static readonly AccessTools.FieldRef<WorldFlowIO,RectTransform> TooltipGraphic=AccessTools.FieldRefAccess<WorldFlowIO,RectTransform>("_miniTooltip");
        private static readonly AccessTools.FieldRef<WorldFlowIO,Sprite[]> InputSprites=AccessTools.FieldRefAccess<WorldFlowIO,Sprite[]>("_inputSprites");
        private static readonly AccessTools.FieldRef<WorldFlowIO,Sprite[]> OutputSprites=AccessTools.FieldRefAccess<WorldFlowIO,Sprite[]>("_outputSprites");
        private static readonly AccessTools.FieldRef<WorldFlowPipe,LineRenderer> Line=AccessTools.FieldRefAccess<WorldFlowPipe,LineRenderer>("_line");
        private static readonly AccessTools.FieldRef<WorldFlowPipe,EdgeCollider2D> Collider=AccessTools.FieldRefAccess<WorldFlowPipe,EdgeCollider2D>("_collider");
        private static FlowElement RequireModel(WorkspaceObject item)=>item?.Native is FlowElement model ? model : throw new ArgumentException("Expected a Foundation building identity.",nameof(item));
        private static T Require<T>(Component view) where T:Component
        { var result=view ? view.GetComponent<T>() : null; return result ? result : throw new ArgumentException("Unexpected Foundation native view.",nameof(view)); }
        public RectTransform PortParentTemplate(WorkspaceObject item)=>PortParent(FlowPrefabSet.Get(RequireModel(item)).Prefab);
        public RectTransform BuildingTemplate(WorkspaceObject item)=>(RectTransform)FlowPrefabSet.Get(RequireModel(item)).Prefab.transform;
        public TMP_Text LabelText(Component buildingView)=>Caption(Require<WorldFlowElement>(buildingView));
        public Image Icon(Component buildingView)=>IconField(Require<WorldFlowElement>(buildingView));
        public Image Background(Component buildingView)=>(Image)AccessTools.Field(typeof(WorldFlowElement),"_background").GetValue(Require<WorldFlowElement>(buildingView));
        public Sprite BackgroundSprite(Component buildingView)=>Require<WorldFlowElement>(buildingView).BackgroundSprite;
        public bool IsOutputFullIndicatorVisible(Component buildingView)
        {var text=AccessTools.Field(typeof(WorldFlowElement),"_outputFull").GetValue(Require<WorldFlowElement>(buildingView)) as TMP_Text;return text && text.gameObject.activeSelf;}
        public void RefreshIcon(Component buildingView,Sprite icon=null)=>Require<WorldFlowElement>(buildingView).UpdateIcon(icon);
        public Component PortTemplate(WorkspaceObject item)=>PortPrefab(FlowPrefabSet.Get(RequireModel(item)).Prefab);
        public Component PortGraphic(Component portView,GamePortGraphic graphic)
        {
            var port=Require<WorldFlowIO>(portView);
            switch(graphic)
            {
                case GamePortGraphic.Sprite:return SpriteGraphic(port);
                case GamePortGraphic.ItemHint:return HintGraphic(port);
                case GamePortGraphic.ItemCount:return CountGraphic(port);
                case GamePortGraphic.MiniTooltip:return TooltipGraphic(port);
                default:throw new ArgumentOutOfRangeException(nameof(graphic));
            }
        }
        public Sprite PortSprite(Component portView,bool input,int index)
        {
            var port=Require<WorldFlowIO>(portView);var sprites=input ? InputSprites(port) : OutputSprites(port);
            if(index<0 || index>=sprites.Length)throw new ArgumentOutOfRangeException(nameof(index));return sprites[index];
        }
        public RectTransform BuildingLabel(Component buildingView)=>Label(Require<WorldFlowElement>(buildingView)).rectTransform;
        public RectTransform GhostLabel(Component ghostView)=>PreviewLabel(Require<WorldFlowGhost>(ghostView)).rectTransform;
        public void ClearPortViewIndex(Component buildingView)=>Ports(Require<WorldFlowElement>(buildingView)).Clear();
        public void RefreshPorts(Component buildingView)=>Require<WorldFlowElement>(buildingView).UpdateIO();
        public Vector2 Size(Component buildingView)=>Require<WorldFlowElement>(buildingView).Size;
        public WorkspacePipe PipeFromView(Component pipeView)=>WorkspacePipe.Wrap(Require<WorldFlowPipe>(pipeView).Pipe);
        public LineRenderer PipeLine(Component pipeView)=>Line(Require<WorldFlowPipe>(pipeView));
        public EdgeCollider2D PipeCollider(Component pipeView)=>Collider(Require<WorldFlowPipe>(pipeView));
        public void RefreshPipe(Component pipeView)=>Require<WorldFlowPipe>(pipeView).UpdateLinePositions();
        public bool HasNativeDragTarget=>DragDropManager.Instance && DragDropManager.Instance.Target!=null;
        public bool IsNativeMoveHandleFor(Component buildingView)=>DragDropManager.Instance && DragDropManager.Instance.Target is WorldFlowLabel handle && handle.GetComponentInParent<WorldFlowElement>()==Require<WorldFlowElement>(buildingView);
    }
}
