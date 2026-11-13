using System.Collections.Generic;
using KingdomFoundation.Json;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Native blueprint primitives. Capture, grouping, validation and placement policy remain in mods.</summary>
    public interface IGameBlueprintAccess
    {
        GameWorkspace DeserializeWorkspace(JsonValue document);
        Component CurrentGhost {get;}
        Component CreateChild(Component parent);
        void SetBuilding(Component ghost,WorkspaceObject item,bool mirror=false);
        void SetDecoration(Component ghost,WorkspaceObject decoration,bool locked=false,bool showCost=false);
        void SetChildOffset(Component ghost,Vector2Int origin);
        IReadOnlyList<Component> Children(Component root);
        WorkspaceObject Building(Component ghost);
        void SetCollision(Component ghost,bool collides);
        bool Overlaps(Vector2Int position,Vector2Int size);
        void UpdateDragPosition();
        void UpdateCostDisplay(GameMoney cost);
    }
}
