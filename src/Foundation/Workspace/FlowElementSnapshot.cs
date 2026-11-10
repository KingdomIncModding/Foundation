using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Items;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Immutable view of one native workspace element at capture time.</summary>
    public sealed class FlowElementSnapshot
    {
        public string Guid { get; }
        public string TypeId { get; }
        public string Label { get; }
        public Vector2Int Position { get; }
        public Vector2Int Size { get; }
        public int StackSize { get; }
        /// <summary>Gets or sets the progress value.</summary>
        public float Progress { get; }
        public float ProductivityProgress { get; }
        public IReadOnlyList<FlowPortSnapshot> Inputs { get; }
        public IReadOnlyList<FlowPortSnapshot> Outputs { get; }

        internal FlowElementSnapshot(FlowElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            Guid = element.Guid;
            TypeId = element.ElementType;
            Label = element.Label;
            Position = element.Position;
            Size = element.Size;
            StackSize = element.StackSize;
            Progress = element.Progress;
            ProductivityProgress = element.ProductivityProgress;
            var inputs = new List<FlowPortSnapshot>(element.Inputs.Count);
            var outputs = new List<FlowPortSnapshot>(element.Outputs.Count);
            foreach (var port in element.Inputs) inputs.Add(new FlowPortSnapshot(port));
            foreach (var port in element.Outputs) outputs.Add(new FlowPortSnapshot(port));
            Inputs = inputs.AsReadOnly();
            Outputs = outputs.AsReadOnly();
        }
    }
}
