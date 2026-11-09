using System;
using System.Collections.Generic;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Definition of a native workspace building type. Its factory must return a fresh model.</summary>
    public interface IWorkspaceNodeDefinition
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        string RequiredTechId { get; }
        ElementCategory Category { get; }
        Vector2Int Size { get; }
        string IconName { get; }
        Sprite Icon { get; }
        double CostMultiplier { get; }
        double GoldMultiplier { get; }
        CurrencyCollection FixedCost { get; }
        Func<WorkspaceNode> Factory { get; }
    }
}
