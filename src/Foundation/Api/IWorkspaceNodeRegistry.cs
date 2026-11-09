using System;
using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public interface IWorkspaceNodeRegistry
    {
        IReadOnlyList<IWorkspaceNodeType> Nodes { get; }
        event Action Changed;
        IWorkspaceNodeType RegisterWorkspaceNode(IWorkspaceNodeDefinition definition);
        bool TryGet(string id, out IWorkspaceNodeType nodeType);
    }
}
