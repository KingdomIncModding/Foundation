using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using UnityEngine;
namespace KingdomFoundation.Machines
{
    public enum MachinePortDirection { Input, Output }

    /// <summary>Immutable reading of a real native FlowIO. Item type is a native registered identifier.</summary>
    public sealed class MachinePortSnapshot
    {
        public MachinePortDirection Direction { get; }
        public int Index { get; }
        public int Position { get; }
        public string ItemTypeId { get; }
        public int BufferCapacity { get; }
        public int BufferCount { get; }
        public bool IsConnected { get; }
        public string ConnectedMachineGuid { get; }
        public int ConnectedPortIndex { get; }
        public bool CanBuffer { get; }
        public bool HasFilter { get; }
        public bool FilterPreview { get; }
        internal MachinePortSnapshot(FlowIO port, MachinePortDirection direction)
        {
            Direction = direction;
            Index = port.ParentIndex;
            Position = port.Position;
            ItemTypeId = port.Item?.Identifier;
            BufferCapacity = port.BufferSize;
            BufferCount = port.BufferedItems.Count;
            CanBuffer = port.CanBuffer;
            HasFilter = port.Filter != null;
            FilterPreview = port.FilterPreview;
            var pipe = port.ConnectedPipe;
            IsConnected = pipe != null;
            var peer = direction == MachinePortDirection.Input ? (FlowIO)pipe?.ConnectedOutput : pipe?.ConnectedInput;
            ConnectedMachineGuid = peer?.Parent?.Guid;
            ConnectedPortIndex = peer?.ParentIndex ?? -1;
        }
    }

    /// <summary>Immutable snapshot; raw native objects never leave through this DTO.</summary>
    public sealed class MachineSnapshot
    {
        public MachineIdentity Identity { get; }
        public string Type { get; }
        public string RuntimeType { get; }
        public string LabelKey { get; }
        public string Category { get; }
        public Vector2Int Position { get; }
        public Vector2Int Size { get; }
        public int StackSize { get; }
        public bool Mirrored { get; }
        public float Progress { get; }
        public float ProductivityProgress { get; }
        public float SpeedMultiplier { get; }
        public float QualityMultiplier { get; }
        public float Productivity { get; }
        public bool CanAutoCraft { get; }
        public bool CanManualCraft { get; }
        public int UpgradeSlots { get; }
        public string SelectedRecipeId { get; }
        public float? NativeRecipeCraftingTime { get; }
        public int TotalInputBuffered { get; }
        public int TotalOutputBuffered { get; }
        public IReadOnlyList<MachinePortSnapshot> Inputs { get; }
        public IReadOnlyList<MachinePortSnapshot> Outputs { get; }
        /// <summary>Exact native class from the source-backed type inventory, or null for mod-created types.</summary>
        public NativeMachineTypeDescriptor NativeType { get; }
        /// <summary>Native machine-specific, scalar/read-only observations. Never the Foundation mod property registry.</summary>
        public IReadOnlyDictionary<string, object> NativeProperties { get; }
        /// <summary>Existing mod-owned extension properties; maintained for binary/source compatibility.</summary>
        public IReadOnlyDictionary<string, object> AdditionalProperties { get; }
        internal MachineSnapshot(FlowElement el, int flow, IReadOnlyDictionary<string, object> extra, IReadOnlyDictionary<string, object> native)
        {
            Identity = new MachineIdentity(el.Guid, flow);
            Type = el.ElementType;
            RuntimeType = el.GetType().FullName;
            LabelKey = el.Label;
            Category = el.Category.ToString();
            Position = el.Position;
            Size = el.Size;
            StackSize = el.StackSize;
            Mirrored = el.Mirrored;
            Progress = el.Progress;
            ProductivityProgress = el.ProductivityProgress;
            SpeedMultiplier = el.Speed;
            QualityMultiplier = el.Quality;
            Productivity = el.Productivity;
            CanAutoCraft = el.CanAutoCraft;
            CanManualCraft = el.CanManualCraft();
            UpgradeSlots = el.UpgradeSlots;
            var recipe = el as Assets.Source.Flow.Elements.RecipeElement;
            SelectedRecipeId = recipe?.Recipe?.Identifier;
            NativeRecipeCraftingTime = recipe?.Recipe == null ? (float?)null : recipe.Recipe.CraftingTime;
            var inList = new List<MachinePortSnapshot>(el.Inputs.Count);
            var outList = new List<MachinePortSnapshot>(el.Outputs.Count);
            int totalInput = 0, totalOutput = 0;
            foreach (var port in el.Inputs) { inList.Add(new MachinePortSnapshot(port, MachinePortDirection.Input)); totalInput += port.BufferedItems.Count; }
            foreach (var port in el.Outputs) { outList.Add(new MachinePortSnapshot(port, MachinePortDirection.Output)); totalOutput += port.BufferedItems.Count; }
            TotalInputBuffered = totalInput; TotalOutputBuffered = totalOutput;
            Inputs = inList.AsReadOnly();
            Outputs = outList.AsReadOnly();
            NativeType = FoundationMachineApi.NativeTypes.Describe(el);
            // Read-only dictionaries contain only captured values (never native mutable collections).
            NativeProperties = native;
            AdditionalProperties = extra;
        }
    }
}
