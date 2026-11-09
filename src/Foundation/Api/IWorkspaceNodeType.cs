using System;
using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>A registered building type, distinct from any placed instance.</summary>
    public interface IWorkspaceNodeType : IDisposable
    {
        string Id { get; }
        IWorkspaceNodeDefinition Definition { get; }
        bool IsRegistered { get; }
        /// <summary>Create an unplaced model. Does not charge currency or add it to a kingdom.</summary>
        WorkspaceNode CreateInstance();
        /// <summary>Open the game's blueprint placement flow after research/funds checks.</summary>
        WorldFlowGhost BeginPlacement(bool hold = false);
    }
}
