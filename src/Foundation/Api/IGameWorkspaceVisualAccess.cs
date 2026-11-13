using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KingdomFoundation.Api
{
    /// <summary>Primitive native view/template access. Layout and rendering policy remain in mods.</summary>
    public interface IGameWorkspaceVisualAccess
    {
        RectTransform PortParentTemplate(WorkspaceObject item);
        RectTransform BuildingTemplate(WorkspaceObject item);
        TMP_Text LabelText(Component buildingView);
        Image Icon(Component buildingView);
        Component BuildingGraphic(Component buildingView,GameBuildingGraphic graphic);
        void UpdateCollider(Component buildingView);
        Image Background(Component buildingView);
        Sprite BackgroundSprite(Component buildingView);
        bool IsOutputFullIndicatorVisible(Component buildingView);
        void RefreshIcon(Component buildingView,Sprite icon=null);
        Component PortTemplate(WorkspaceObject item);
        Component PortGraphic(Component portView, GamePortGraphic graphic);
        Sprite PortSprite(Component portView, bool input, int index);
        RectTransform BuildingLabel(Component buildingView);
        RectTransform GhostLabel(Component ghostView);
        void ClearPortViewIndex(Component buildingView);
        void RefreshPorts(Component buildingView);
        Vector2 Size(Component buildingView);
        WorkspacePipe PipeFromView(Component pipeView);
        LineRenderer PipeLine(Component pipeView);
        EdgeCollider2D PipeCollider(Component pipeView);
        void RefreshPipe(Component pipeView);
        bool HasNativeDragTarget { get; }
        bool IsNativeMoveHandleFor(Component buildingView);
    }
}
