using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Native FlowUpgrade.Create resolves only types from the base game's namespace.
    /// Foundation patches that exact factory for registered custom implementations,
    /// so native upgrade save/load and CreateCopy continue to work.
    /// </summary>
    public interface IGameUpgradeRegistry
    {
        IReadOnlyList<string> RegisteredTypes { get; }
        void Register(string modId, string typeId, Func<FlowUpgrade> factory);
        bool TryCreate(string typeId, out FlowUpgrade upgrade);
    }
}
