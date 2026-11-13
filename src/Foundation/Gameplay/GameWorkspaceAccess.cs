using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KingdomFoundation.Api
{
    internal sealed class GameWorkspaceAccess : IGameWorkspaceAccess
    {
        private static readonly ConditionalWeakTable<object, WorkspaceObject> Identities = new ConditionalWeakTable<object, WorkspaceObject>();
        private static WorkspaceObject BareIdentity(object native)
        {
            if (native is FlowElement element)
                return new WorkspaceObject(element, WorkspaceObjectKind.Building, element.ElementType, element.ElementType, null, Math.Max(1, element.StackSize), false);
            var deco = (FlowDeco)native;
            var kind = deco.Sticker != null ? WorkspaceObjectKind.Sticker : deco.Label != null ? WorkspaceObjectKind.Label : WorkspaceObjectKind.Container;
            return new WorkspaceObject(deco, kind, kind.ToString(), kind.ToString(), null, 1, deco.Locked);
        }

        internal static WorkspaceObject IdentityFor(FlowElement element) => Identities.GetValue(element, BareIdentity);
        internal static WorkspaceObject IdentityFor(FlowDeco decoration) => Identities.GetValue(decoration, BareIdentity);
        private static WorkspaceObject Identity(object native, WorkspaceObjectKind kind, string typeId, string name, Sprite icon, int stackSize, bool locked)
        {
            var handle = Identities.GetValue(native, BareIdentity);
            handle.Refresh(kind, typeId, name, icon, stackSize, locked);
            return handle;
        }

        private static readonly AccessTools.FieldRef<WorldFlowElement, Image> IconField = AccessTools.FieldRefAccess<WorldFlowElement, Image>("_icon");
        public bool IsReady => WorldFlowManager.Instance && FlowColliderManager.Instance && FoundationApi.World.ActiveFlow != null;
        public Component Root => WorldFlowManager.Instance;
        public IReadOnlyList<Component> PipeNodeViews => ((RectTransform)AccessTools.Field(typeof(WorldFlowManager), "_pipeParent").GetValue(WorldFlowManager.Instance)).GetComponentsInChildren<WorldFlowPipeNode>(true).Cast<Component>().ToArray();

        public void ClearViewCaches()
        {
            ((List<WorldFlowElement>)AccessTools.Field(typeof(WorldFlowManager), "_elements").GetValue(WorldFlowManager.Instance)).Clear();
            ((List<WorldFlowDeco>)AccessTools.Field(typeof(WorldFlowDeco), "_activeContainers").GetValue(null)).Clear();
        }

        public void SetupWorkspace(int index) => WorldFlowManager.Instance.SetupFlow(index);
        public void ShowWorkspace(int index) => WorldFlowManager.Instance.ShowFlow(index);
        public IReadOnlyList<WorkspaceObject> Catalog => Array.AsReadOnly(FoundationApi.Machines.Catalog.Select(Describe).ToArray());

        public IReadOnlyList<WorkspaceObject> DiscoverCatalog()
        {
            if (FoundationApi.World.IsInKingdom)
                return Catalog;
            var previous = Assets.Source.Player.GamePlayer.Current;
            try
            {
                Assets.Source.Player.GamePlayer.Current = new Assets.Source.Player.GamePlayer();
                Assets.Source.Player.GamePlayer.Current.Flows.Add(new FlowDiagram());
                return Array.AsReadOnly(FlowPrefabSet.Ordered.Select(prefab => Describe(FlowElement.Create(prefab.Identifier))).ToArray());
            }
            finally
            {
                Assets.Source.Player.GamePlayer.Current = previous;
            }
        }

        public WorkspaceObject Create(string typeId) => IdentityFor(FlowElement.Create(typeId));
        public bool CanPurchase(WorkspaceObject item) => FoundationApi.World.IsInKingdom && RequireMachine(item).IsPurchaseable(Assets.Source.Player.GamePlayer.Current);
        public GameMoney PurchaseCost(WorkspaceObject item) => new GameMoney(RequireMachine(item).GetPurchaseCost());
        public void StopBlueprintMode() => GameWorldService.RequireManager().StopBlueprintMode();
        public Component ShowPurchaseBlueprint(WorkspaceObject item, bool repeat = false) => GameWorldService.RequireManager().ShowPurchaseBlueprint(RequireMachine(item), repeat);
        public IReadOnlyList<WorkspaceObject> ActiveObjects
        {
            get
            {
                var flow = FoundationApi.World.ActiveFlow;
                return flow == null ? Array.Empty<WorkspaceObject>() : Array.AsReadOnly(flow.Elements.Select(Describe).Concat(flow.Decorations.Select(Describe)).ToArray());
            }
        }

        public WorkspaceObject FromView(Component view) => view is WorldFlowElement element && element.Element != null ? Describe(element.Element) : view is WorldFlowDeco deco && deco.Decoration != null ? Describe(deco.Decoration) : null;
        public KingdomFoundation.Json.JsonValue Serialize(WorkspaceObject item)
        {
            if (item?.Native is FlowElement element)
                return new KingdomFoundation.Json.JsonValue(element.ToJson());
            if (item?.Native is FlowDeco deco)
                return new KingdomFoundation.Json.JsonValue(deco.ToJson());
            throw new ArgumentException("Expected a Foundation workspace identity.", nameof(item));
        }

        public WorkspaceObject Deserialize(KingdomFoundation.Json.JsonValue data)
        {
            var model = FlowElement.FromJson(data.Native, null);
            return model == null ? null : IdentityFor(model);
        }

        public WorkspaceObject Copy(WorkspaceObject item)
        {
            if (item?.Native is FlowElement element)
            {
                var model = element.CreateCopy();
                return model == null ? null : IdentityFor(model);
            }

            if (item?.Native is FlowDeco deco)
            {
                var model = deco.CreateCopy();
                return model == null ? null : IdentityFor(model);
            }

            throw new ArgumentException("Expected a Foundation workspace identity.", nameof(item));
        }

        public UnityEngine.Behaviour FindBuildingView(GameObject view) => view ? view.GetComponentInParent<WorldFlowElement>() : null;
        public IReadOnlyList<WorkspaceObject> AllObjects => FoundationApi.World.IsInKingdom ? Array.AsReadOnly(FoundationApi.World.Flows.SelectMany(flow => flow.Elements.Select(Describe).Concat(flow.Decorations.Select(Describe))).ToArray()) : Array.Empty<WorkspaceObject>();

        public bool IsMachineExtensionType(Type extensionType) => extensionType != null && extensionType.Assembly != typeof(FlowElement).Assembly && typeof(FlowElement).IsAssignableFrom(extensionType);
        public bool IsExtensionInstance(WorkspaceObject item, Type extensionType) => item != null && IsMachineExtensionType(extensionType) && extensionType.IsInstanceOfType(item.Native);
        public object ReadExtensionProperty(WorkspaceObject item, PropertyInfo property)
        {
            if (property == null || property.DeclaringType?.Assembly == typeof(FlowElement).Assembly || property.GetGetMethod() == null || property.GetIndexParameters().Length != 0)
                throw new ArgumentException("Expected a public property declared by an external mod, not the game.", nameof(property));
            if (item == null || !property.DeclaringType.IsInstanceOfType(item.Native))
                throw new ArgumentException("Property does not belong to this extension instance.", nameof(item));
            global::System.Object value = property.GetValue(item.Native);
            return GameValueCodec.Wrap(value);
        }

        public GameWorkspace ExtensionWorkspace(WorkspaceObject item, string propertyName)
        {
            if (item == null || item.Native.GetType().Assembly == typeof(FlowElement).Assembly)
                return null;
            var property = item.Native.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property == null ? null : ReadExtensionProperty(item, property) as GameWorkspace;
        }

        public object InvokeExtension(MethodInfo method, params object[] arguments)
        {
            if (method == null || !method.IsStatic || method.DeclaringType?.Assembly == typeof(FlowElement).Assembly)
                throw new ArgumentException("Expected a static method declared by an external mod.", nameof(method));
            return GameValueCodec.Wrap(method.Invoke(null, arguments.Select(GameValueCodec.Unwrap).ToArray()));
        }

        public IReadOnlyList<WorkspaceObject> Overlap(Vector2Int topLeft, Vector2Int size)
        {
            var result = new List<WorkspaceObject>();
            if (!IsReady)
                return result.AsReadOnly();
            var seen = new HashSet<object>();
            foreach (var collider in FlowColliderManager.Instance.GetOverlap(topLeft, size))
            {
                if (!collider)
                    continue;
                var flowCollider = collider.GetComponent<FlowCollider>();
                var machine = collider.GetComponent<WorldFlowElement>() ?? flowCollider?.Element;
                if (machine && machine.Element != null && FoundationApi.World.ActiveFlow.Elements.Contains(machine.Element))
                {
                    if (seen.Add(machine.Element))
                        result.Add(Describe(machine.Element));
                    continue;
                }

                var deco = collider.GetComponent<WorldFlowDeco>() ?? flowCollider?.Deco;
                if (deco && deco.Decoration != null && FoundationApi.World.ActiveFlow.Decorations.Contains(deco.Decoration) && seen.Add(deco.Decoration))
                    result.Add(Describe(deco.Decoration));
            }

            return result.AsReadOnly();
        }

        internal static WorkspaceObject Describe(FlowElement element)
        {
            var icon = string.IsNullOrEmpty(element.IconName) ? null : FoundationApi.Presentation.FindSprite(element.IconName);
            if (!icon && element.World)
                icon = IconField(element.World)?.sprite;
            if (!icon)
                icon = element.Icon;
            if (!icon)
            {
                var match = FoundationApi.Machines.Catalog.FirstOrDefault(e => e.ElementType == element.ElementType);
                if (!string.IsNullOrEmpty(match?.IconName))
                    icon = FoundationApi.Presentation.FindSprite(match.IconName);
            }

            var name = string.IsNullOrEmpty(element.Label) ? element.ElementType : FoundationApi.Presentation.Translate(element.Label);
            return Identity(element, WorkspaceObjectKind.Building, element.ElementType ?? element.GetType().FullName, string.IsNullOrWhiteSpace(name) ? element.ElementType : name, icon, Math.Max(1, element.StackSize), false);
        }

        private static WorkspaceObject Describe(FlowDeco deco)
        {
            var kind = deco.Sticker != null ? WorkspaceObjectKind.Sticker : deco.Label != null ? WorkspaceObjectKind.Label : WorkspaceObjectKind.Container;
            var token = kind == WorkspaceObjectKind.Label ? "@deco_label" : "@deco_rect";
            var name = kind == WorkspaceObjectKind.Sticker ? "Sticker" : FoundationApi.Presentation.Translate(token);
            var icon = deco.Sticker?.GetSprite() ?? FoundationApi.Presentation.FindSprite(kind == WorkspaceObjectKind.Label ? "Icons_32" : kind == WorkspaceObjectKind.Sticker ? "Icons2_45" : "Icons_31");
            return Identity(deco, kind, kind.ToString(), string.IsNullOrWhiteSpace(name) || name == token ? kind.ToString() : name, icon, 1, deco.Locked);
        }

        public bool IsInActiveWorkspace(WorkspaceObject item)
        {
            var flow = FoundationApi.World.ActiveFlow;
            if (flow == null || item == null)
                return false;
            return item.Native is FlowElement element ? flow.Elements.Contains(element) : item.Native is FlowDeco deco && flow.Decorations.Contains(deco) && deco.World;
        }

        public GameMoney Refund(WorkspaceObject item, int alreadyRefundedUnits = 0, bool includeAnchored = false)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return new GameMoney(item.Native is FlowElement element ? element.GetRefund(alreadyRefundedUnits) : ((FlowDeco)item.Native).GetRefund(includeAnchored));
        }

        public void Remove(WorkspaceObject item)
        {
            if (!IsInActiveWorkspace(item))
                throw new InvalidOperationException("Object is not in the active workspace.");
            if (item.Native is FlowElement element)
                FoundationApi.Workspace.Remove(element);
            else
                FoundationApi.Workspace.RemoveDecoration((FlowDeco)item.Native);
        }

        public void Grant(GameMoney amounts, bool countAsIncome = false)
        {
            if (amounts == null)
                throw new ArgumentNullException(nameof(amounts));
            FoundationApi.Economy.Grant(amounts.Native, countAsIncome);
        }

        private static FlowElement RequireMachine(WorkspaceObject item) => item?.Native is FlowElement element ? element : throw new ArgumentException("Expected a Foundation machine identity.", nameof(item));
        private static WorldFlowElement RequireView(Component view) => view is WorldFlowElement element ? element : throw new ArgumentException("Expected a Foundation building view.", nameof(view));
        public GameMoney SingleCopyCost(WorkspaceObject item, int countOffset = 0) => new GameMoney(RequireMachine(item).GetSingleCopyCost(countOffset));
        public GameMoney CopyCost(WorkspaceObject item, int countOffset = 0, bool includeAnchored = false) => new GameMoney(item.Native is FlowElement element ? element.GetCopyCost(countOffset) : ((FlowDeco)item.Native).GetCopyCost(includeAnchored));
        public void SetStackSize(WorkspaceObject item, int size)
        {
            var element = RequireMachine(item);
            if (!IsInActiveWorkspace(item))
                throw new InvalidOperationException("Machine is not in the active workspace.");
            if (!element.Stackable || size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            element.StackSize = size;
            element.UpdateUpgrades();
            if (!element.World)
                return;
            // Native SetStackSize does not retain the indicator root in game 1.0.14.
            foreach (Transform child in element.World.transform)
            {
                if (child.name != "StackSizeIndicator")
                    continue;
                child.gameObject.SetActive(false);
                global::UnityEngine.Object.Destroy(child.gameObject);
            }

            element.World.SetStackSize(size);
        }

        public void RefreshLabel(Component view) => RequireView(view).UpdateLabel();
        public void ShowIncome(Component view, GameMoney amounts)
        {
            if (amounts == null)
                throw new ArgumentNullException(nameof(amounts));
            var native = RequireView(view);
            foreach (var entry in amounts.Native.GetCurrency())
                native.ShowIncome(entry.c, entry.amt, force: true);
        }

        public void ShowMessage(Component view, string message, Color color) => RequireView(view).ShowMessage(message, color);
    }
}
