using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    internal sealed class GameUpgradeRegistry : IGameUpgradeRegistry
    {
        readonly Dictionary<string, Func<FlowUpgrade>> factories = new Dictionary<string, Func<FlowUpgrade>>(StringComparer.Ordinal);
        /// <summary>Gets or sets the registered types value.</summary>
        public IReadOnlyList<string> RegisteredTypes => Array.AsReadOnly(new List<string>(factories.Keys).ToArray());
        public void Register(string modId, string typeId, Func<FlowUpgrade> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(typeId) ||
                !typeId.StartsWith(modId + ":", StringComparison.Ordinal))
                throw new ArgumentException("Upgrade type must use a namespaced 'modId:name' ID.");
            if (factories.ContainsKey(typeId)) throw new ArgumentException("Duplicate upgrade type: " + typeId);
            // Factory validation is performed before registry mutation.
            var preview = factory() ?? throw new ArgumentException("Upgrade factory returned null.");
            if (preview.UpgradeType != typeId)
                throw new ArgumentException("Upgrade.UpgradeType must return the registered type ID (" + typeId + ").");
            factories.Add(typeId, factory);
        }
        /// <summary>Attempts to create.</summary>
        public bool TryCreate(string typeId, out FlowUpgrade upgrade)
        {
            upgrade = null;
            if (typeId == null || !factories.TryGetValue(typeId, out var factory)) return false;
            upgrade = factory();
            if (upgrade == null || upgrade.UpgradeType != typeId)
                throw new InvalidOperationException("Upgrade factory failed to return its declared type " + typeId);
            return true;
        }
    }
}
