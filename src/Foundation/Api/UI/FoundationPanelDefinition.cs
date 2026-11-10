using System;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    /// <summary>One mod-owned, declarative UI panel, optionally bound to a native machine type.</summary>
    public sealed class FoundationPanelDefinition
    {
        public string ModId { get; }
        public string Id { get; }
        public string Title { get; }
        public Type MachineType { get; }
        public int Priority { get; }
        public Action<IFoundationPanelBuilder> Build { get; }
        /// <summary>Gets or sets the allow drag value.</summary>
        public bool AllowDrag { get; }

        public FoundationPanelDefinition(string modId, string id, string title,
            Action<IFoundationPanelBuilder> build, Type machineType = null, int priority = 0,
            bool allowDrag = true)
        {
            ModBarRegistry.ValidateIdentity(modId, id);
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A panel title is required.", nameof(title));
            if (build == null) throw new ArgumentNullException(nameof(build));
            if (machineType != null && (!typeof(FlowElement).IsAssignableFrom(machineType) || !machineType.IsClass))
                throw new ArgumentException("Machine UI requires a native FlowElement-derived class.", nameof(machineType));
            ModId = modId;
            Id = id;
            Title = title;
            MachineType = machineType;
            Priority = priority;
            Build = build;
            AllowDrag = allowDrag;
        }
    }
}
