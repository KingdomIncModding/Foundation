using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Stable workspace identity and primitive counts, safe for serialization observers.</summary>
    public sealed class GameWorkspace
    {
        private static readonly ConditionalWeakTable<FlowDiagram, GameWorkspace> Cache = new ConditionalWeakTable<FlowDiagram, GameWorkspace>();
        private readonly IReadOnlyList<WorkspaceObject> buildings;
        private readonly IReadOnlyList<WorkspacePipe> pipes;
        private readonly IReadOnlyList<WorkspaceObject> decorations;
        internal FlowDiagram Native { get; }
        private GameWorkspace(FlowDiagram native)
        {
            Native = native;
            buildings = new WorkspaceReadOnlyList<FlowElement, WorkspaceObject>(native.Elements, GameWorkspaceAccess.IdentityFor);
            pipes = new WorkspaceReadOnlyList<FlowPipe, WorkspacePipe>(native.Pipes, WorkspacePipe.Wrap);
            decorations = new WorkspaceReadOnlyList<FlowDeco, WorkspaceObject>(native.Decorations, GameWorkspaceAccess.IdentityFor);
        }
        internal static GameWorkspace Wrap(FlowDiagram native) => native == null ? null : Cache.GetValue(native, value => new GameWorkspace(value));
        public string Label => Native.Label;
        public UnityEngine.Vector2Int CameraPosition {get=>Native.CameraPos;set=>Native.CameraPos=value;}
        public int BuildingCount => Native.Elements.Count;
        public int PipeCount => Native.Pipes.Count;
        public int DecorationCount => Native.Decorations.Count;
        public int RouteNodeCount { get { int count = 0; foreach (var pipe in Native.Pipes) count += pipe.Nodes.Count; return count; } }
        /// <summary>Live read-only buildings. Take an explicit snapshot before editing the collection during enumeration.</summary>
        public IReadOnlyList<WorkspaceObject> Buildings => buildings;
        /// <summary>Live read-only pipes. Take an explicit snapshot before editing the collection during enumeration.</summary>
        public IReadOnlyList<WorkspacePipe> Pipes => pipes;
        /// <summary>Live read-only decorations. Take an explicit snapshot before editing the collection during enumeration.</summary>
        public IReadOnlyList<WorkspaceObject> Decorations => decorations;
        public void SetBuildings(IEnumerable<WorkspaceObject> values)
        { var items=values.Select(value=>(FlowElement)value.Native).ToArray(); Native.Elements.Clear(); Native.Elements.AddRange(items); }
        public void SetPipes(IEnumerable<WorkspacePipe> values)
        { var items=values.Select(value=>value.Native).ToArray(); Native.Pipes.Clear(); Native.Pipes.AddRange(items); }
        public void SetDecorations(IEnumerable<WorkspaceObject> values)
        { var items=values.Select(value=>(FlowDeco)value.Native).ToArray(); Native.Decorations.Clear(); Native.Decorations.AddRange(items); }
    }
}
