namespace KingdomFoundation.Api
{
    /// <summary>Stable hook identifiers. Native target names are owned by Foundation.</summary>
    public static class GameHookPoints
    {
        public const string PipeBound="pipe.bound";
        public const string PipeDragMove = "pipe.drag.move";
        public const string PipeDragEnd = "pipe.drag.end";
        public const string PipeDragCancel = "pipe.drag.cancel";
        public const string PipeItemTransferred="pipe.item-transferred";
        public const string PipeItemRejected="pipe.item-rejected";
        public const string WorkspaceShown="workspace.shown";
        public const string InputInitialized="input.initialized";
        public const string BindingRowBound="binding.row-bound";
        public const string GhostCommit="ghost.commit";
        public const string GhostCost="ghost.cost";
        public const string CurrencyAdded="kingdom.currency-added";
        public const string CurrencySpent="kingdom.currency-spent";
        public const string PortConsumed="port.consumed";
        public const string UiVisibility="ui.visibility";
        public const string PortAcceptance="port.acceptance";
        public const string PortDelivery="port.delivery";
        public const string BuildingClearPorts="building.clear-ports";
        public const string DeconstructionUpdate = "ui.deconstruction.update";
        public const string DeconstructionVisibility = "ui.deconstruction.visibility";
        public const string Cancel = "ui.cancel";
        public const string Escape = "ui.escape";
        public const string GridSnap = "workspace.grid.snap";
        public const string CameraUpdate = "view.camera.update";
        public const string CameraChangeZoom = "view.camera.change-zoom";
        public const string CameraSetZoom = "view.camera.set-zoom";
        public const string CameraZoomScale = "view.camera.zoom-scale";
        public const string BuildingClick = "workspace.building.click";
        public const string BuildingCanStack = "workspace.building.can-stack";
        public const string BuildingBound = "workspace.building.bound";
        public const string BuildingUpdatePorts = "workspace.building.update-ports";
        public const string BuildingSize = "workspace.building.size";
        public const string BuildingSerialize = "workspace.building.serialize";
        public const string BuildingDeserialize = "workspace.building.deserialize";
        public const string BuildingCopy = "workspace.building.copy";
        public const string PipeUpdateGeometry = "workspace.pipe.update-geometry";
        public const string PortBound = "workspace.port.bound";
        public const string GhostBound = "workspace.ghost.bound";
        public const string DecorationSerialize = "workspace.decoration.serialize";
        public const string DecorationDeserialize = "workspace.decoration.deserialize";
        public const string DecorationCopy = "workspace.decoration.copy";
        public const string DecorationStyleUpdate = "workspace.decoration.update-style";
        public const string DecorationPopupOpen = "ui.decoration.open";
        public const string DecorationStyleSelected = "ui.decoration.style-selected";
        public const string DecorationSelectionUpdate = "ui.decoration.selection-update";
        public const string ResearchCost = "research.cost";
        public const string PlayerHasResearch = "kingdom.has-research";
        public const string KingdomDeserialized = "kingdom.deserialized";
        public const string SpawnerConstructed = "workspace.spawner.constructed";
        public const string SpawnerIcon = "workspace.spawner.icon";
        public const string MenuUpdate="ui.main-menu.update";
        public const string UiStarted="ui.started";
        public const string BindingRowClick="input.binding-row.click";
        public const string BindingRowText="input.binding-row.text";
        public const string BindingsApply="input.bindings.apply";
        public const string BindingsCancel="input.bindings.cancel";
        public const string BindingsReset="input.bindings.reset";
        public const string BindingsLoad="input.bindings.load";
        public const string BindingsOpened="input.bindings.opened";
    }
}
