using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal static class NativeResearch
    {
        static readonly Dictionary<string, TechNode> owned = new Dictionary<string, TechNode>(StringComparer.Ordinal);
        static readonly Dictionary<string, TechNodeRegistration> live = new Dictionary<string, TechNodeRegistration>(StringComparer.Ordinal);
        static readonly System.Reflection.FieldInfo PlayerFlags = AccessTools.Field(typeof(GamePlayer), "_techUnlocked");

        internal static bool Owns(TechNode node) => node != null && owned.TryGetValue(node.Identifier, out var model) && ReferenceEquals(model, node);
        internal static bool Active(TechNode node) => Owns(node) && live.TryGetValue(node.Identifier, out var registration) && registration.IsRegistered;
        internal static ITechMod ModOf(TechNode node) => Active(node) ? live[node.Identifier].Mod : null;
        internal static TechNode Register(TechNodeRegistration registration, TechNodeDefinition definition)
        {
            // BepInEx Awake can precede Bootstrap.InitializeGame. The native tech
            // constructors require the game's locale; use its idempotent initializer.
            Assets.Source.Util.Translation.Init();
            ValidateLinks(definition);
            var node = TechNode.Get(definition.Id);
            if (node != null && !Owns(node)) throw new ArgumentException("This identifier belongs to existing game research: " + definition.Id);
            if (node == null) { node = TechNode.Add(new TechNode(definition.Id) { CostTier = definition.Research.CostTier }); owned.Add(definition.Id, node); }
            registration.NativeNode = node;
            Apply(registration, definition);
            live[definition.Id] = registration;
            EnsureCapacity(GamePlayer.Current);
            return node;
        }
        static void ValidateLinks(TechNodeDefinition definition)
        {
            var settings = definition.Research;
            foreach (var id in new[] { settings.RootId, settings.PreviousId }.Where(id => id != null))
            {
                var linked = TechNode.Get(id);
                if (id == definition.Id || !Active(linked)) throw new ArgumentException("Create the parent/previous node on Mods first: " + id);
                // Reparenting an ancestor under a descendant would make native Position/root navigation recursive.
                for (var parent = linked; parent != null; parent = parent.Root)
                    if (parent.Identifier == definition.Id) throw new ArgumentException("Research links cannot form a cycle.");
                var visited = new HashSet<TechNode>();
                for (var previous = linked; previous != null && visited.Add(previous); previous = previous.Previous)
                    if (previous.Identifier == definition.Id) throw new ArgumentException("Research links cannot form a cycle.");
            }
            foreach (var id in settings.Requirements)
                if (id == definition.Id || TechNode.Get(id) == null) throw new ArgumentException("Unknown or self-referencing requirement: " + id);
            // Resolve the icon before changing a live node, so invalid updates are atomic.
            if (!definition.Icon) SpriteLibrary.Get(settings.IconName);
        }
        internal static void Apply(TechNodeRegistration registration, TechNodeDefinition definition)
        {
            ValidateLinks(definition);
            foreach (var id in new[] { definition.Research.RootId, definition.Research.PreviousId }.Where(id => id != null))
                if (ModOf(TechNode.Get(id))?.Id != registration.Mod.Id)
                    throw new ArgumentException("Branch parents must belong to the same mod. Use Requirements for dependencies on other mods.");
            var node = registration.NativeNode;
            var settings = definition.Research;
            node.Name = definition.Title; node.Description = definition.Description;
            node.DevMode = false;
            node.AbsolutePosition = Vector2Int.RoundToInt(definition.Position); node.RelativePosition = Vector2Int.zero;
            node.Previous = settings.PreviousId == null ? null : TechNode.Get(settings.PreviousId);
            node.NodeType = settings.NodeType; node.Root = settings.RootId == null ? node.Previous?.Root : TechNode.Get(settings.RootId);
            node.ForceRootNode = node.Root == null;
            node.AdditionalRequirements = settings.Requirements.ToList();
            AccessTools.Field(typeof(TechNode), "_additionalRequirements").SetValue(node, null);
            node.CostTier = settings.CostTier; node.CostCurrency = CurrencyCollection.GetDefaultCostCurrency(settings.CostTier);
            node.CostMultiplier = settings.CostMultiplier; node.GoldMultiplier = settings.GoldMultiplier;
            node.ResetCost(); node.Cost = settings.FixedCost;
            node.IconName = definition.Icon ? "foundation:" + definition.Id : settings.IconName;
            node.Hidden = player => !registration.IsRegistered ||
                (node.Root != null && !Active(node.Root)) || (node.Previous != null && !Active(node.Previous));
            node.OnUnlock = player => registration.Click();
        }
        internal static bool TryIcon(string name, out Sprite sprite)
        {
            sprite = null;
            if (name == null || !name.StartsWith("foundation:", StringComparison.Ordinal)) return false;
            var id = name.Substring("foundation:".Length);
            if (!live.TryGetValue(id, out var node)) return false;
            sprite = node.Definition.Icon; return sprite;
        }
        internal static void Deactivate(TechNodeRegistration registration)
        {
            if (!live.TryGetValue(registration.Id, out var current) || !ReferenceEquals(current, registration)) return;
            registration.NativeNode.DevMode = true;
            registration.NativeNode.IconName = ((INativeTechNodeDefinition)registration.Definition).Research.IconName;
            registration.NativeNode.OnUnlock = null;
            live.Remove(registration.Id);
        }
        internal static void RestoreFixedCost(TechNode node)
        {
            if (!Active(node)) return;
            var cost = ((INativeTechNodeDefinition)live[node.Identifier].Definition).Research.FixedCost;
            if (cost != null) node.Cost = cost;
        }
        internal static void EnsureCapacity(GamePlayer player)
        {
            if (player == null) return;
            var flags = (bool[])PlayerFlags.GetValue(player);
            if (flags.Length >= TechNode.Count) return;
            Array.Resize(ref flags, TechNode.Count); PlayerFlags.SetValue(player, flags);
        }
    }
}
