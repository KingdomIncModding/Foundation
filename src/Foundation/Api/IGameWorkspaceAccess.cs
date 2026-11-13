using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace KingdomFoundation.Api
{
    /// <summary>Primitive native spatial/model access. Selection and batch policy belong to consumers.</summary>
    public interface IGameWorkspaceAccess
    {
        bool IsReady { get; }
        Component Root { get; }
        IReadOnlyList<Component> PipeNodeViews { get; }
        void ClearViewCaches();
        void SetupWorkspace(int index);
        void ShowWorkspace(int index);
        IReadOnlyList<WorkspaceObject> Catalog { get; }
        IReadOnlyList<WorkspaceObject> ActiveObjects { get; }
        IReadOnlyList<WorkspaceObject> AllObjects { get; }
        IReadOnlyList<WorkspaceObject> DiscoverCatalog();
        WorkspaceObject Create(string typeId);
        bool CanPurchase(WorkspaceObject item);
        GameMoney PurchaseCost(WorkspaceObject item);
        void StopBlueprintMode();
        Component ShowPurchaseBlueprint(WorkspaceObject item,bool repeat=false);
        WorkspaceObject FromView(Component view);
        KingdomFoundation.Json.JsonValue Serialize(WorkspaceObject item);
        WorkspaceObject Deserialize(KingdomFoundation.Json.JsonValue data);
        WorkspaceObject Copy(WorkspaceObject item);
        UnityEngine.Behaviour FindBuildingView(GameObject view);
        bool IsMachineExtensionType(Type extensionType);
        bool IsExtensionInstance(WorkspaceObject item, Type extensionType);
        object ReadExtensionProperty(WorkspaceObject item, PropertyInfo property);
        object InvokeExtension(MethodInfo method,params object[] arguments);
        GameWorkspace ExtensionWorkspace(WorkspaceObject item,string propertyName);
        IReadOnlyList<WorkspaceObject> Overlap(Vector2Int topLeft, Vector2Int size);
        bool IsInActiveWorkspace(WorkspaceObject item);
        GameMoney Refund(WorkspaceObject item, int alreadyRefundedUnits = 0, bool includeAnchored = false);
        void Remove(WorkspaceObject item);
        void Grant(GameMoney amounts, bool countAsIncome = false);
        GameMoney SingleCopyCost(WorkspaceObject item, int countOffset = 0);
        GameMoney CopyCost(WorkspaceObject item,int countOffset=0,bool includeAnchored=false);
        void SetStackSize(WorkspaceObject item, int size);
        void RefreshLabel(Component view);
        void ShowIncome(Component view, GameMoney amounts);
        void ShowMessage(Component view, string message, Color color);
    }
}
