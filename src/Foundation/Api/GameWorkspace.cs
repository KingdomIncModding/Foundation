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
        internal FlowDiagram Native { get; }
        private GameWorkspace(FlowDiagram native) { Native = native; }
        internal static GameWorkspace Wrap(FlowDiagram native) => native == null ? null : Cache.GetValue(native, value => new GameWorkspace(value));
        public string Label => Native.Label;
        public UnityEngine.Vector2Int CameraPosition {get=>Native.CameraPos;set=>Native.CameraPos=value;}
        public int BuildingCount => Native.Elements.Count;
        public int PipeCount => Native.Pipes.Count;
        public int DecorationCount => Native.Decorations.Count;
        public int RouteNodeCount { get { int count = 0; foreach (var pipe in Native.Pipes) count += pipe.Nodes.Count; return count; } }
        public IReadOnlyList<WorkspaceObject> Buildings => Native.Elements.Select(GameWorkspaceAccess.IdentityFor).ToArray();
        public IReadOnlyList<WorkspacePipe> Pipes => Native.Pipes.Select(WorkspacePipe.Wrap).ToArray();
        public IReadOnlyList<WorkspaceObject> Decorations => Native.Decorations.Select(GameWorkspaceAccess.IdentityFor).ToArray();
        public void SetBuildings(IEnumerable<WorkspaceObject> values)
        { var items=values.Select(value=>(FlowElement)value.Native).ToArray(); Native.Elements.Clear(); Native.Elements.AddRange(items); }
        public void SetPipes(IEnumerable<WorkspacePipe> values)
        { var items=values.Select(value=>value.Native).ToArray(); Native.Pipes.Clear(); Native.Pipes.AddRange(items); }
        public void SetDecorations(IEnumerable<WorkspaceObject> values)
        { var items=values.Select(value=>(FlowDeco)value.Native).ToArray(); Native.Decorations.Clear(); Native.Decorations.AddRange(items); }
    }
}
