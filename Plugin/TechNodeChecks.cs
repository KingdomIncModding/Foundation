using System;
using System.Linq;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        ITechNode diagnosticNode;
        static int FoundationApiNodeCount() => FoundationApi.TechNodes.Nodes.Count;
        void Throws<T>(Action action, string name) where T : Exception
        {
            try { action(); }
            catch (T) { Check(true, name); return; }
            throw new InvalidOperationException(name + ": expected " + typeof(T).Name);
        }
        sealed class MutableDefinition : ITechNodeDefinition
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public Sprite Icon { get; set; }
            public Vector2 Position { get; set; }
        }
        void VerifyRegistry()
        {
            ITechNodeRegistry registry = new TechNodeRegistry();
            int changes = 0; registry.Changed += () => changes++;
            Throws<ArgumentNullException>(() => registry.CreateTechNode(null), "Null definitions rejected");
            Throws<ArgumentException>(() => new TechNodeDefinition(" ", "Title"), "Blank identifiers rejected");
            Throws<ArgumentException>(() => new TechNodeDefinition("example.one", " "), "Blank titles rejected");
            Throws<ArgumentOutOfRangeException>(() => new TechNodeDefinition("example.one", "Title", new Vector2(float.NaN, 0)), "Nonfinite positions rejected");
            var definition = new MutableDefinition { Id = "example.first", Title = "First" };
            var first = registry.CreateTechNode(definition);
            definition.Title = "Changed behind registry";
            Check(first.Definition.Title == "First" && first.Definition.Description == "", "Custom interface definitions are validated and snapshotted");
            var snapshot = registry.Nodes;
            var second = registry.CreateTechNode(new TechNodeDefinition("other.second", "Second"));
            Check(snapshot.Count == 1 && registry.Nodes.Count == 2, "Registry enumerations are stable snapshots");
            Check(registry.TryGet(first.Id, out var found) && ReferenceEquals(found, first), "Identifier lookup returns original interface handle");
            Throws<ArgumentException>(() => registry.CreateTechNode(new TechNodeDefinition(first.Id, "Collision")), "Duplicate identifiers preserve the owning mod's registration");
            Check(registry.Nodes.Count == 2 && ReferenceEquals(registry.Nodes[0], first), "Rejected duplicate does not replace existing node");
            first.Update(new TechNodeDefinition(first.Id, "Updated", new Vector2(200, 100)));
            Check(first.Definition.Title == "Updated" && first.Definition.Position == new Vector2(200, 100) && changes == 3, "Update changes definition and notifies subscribers once");
            Throws<ArgumentException>(() => first.Update(new TechNodeDefinition("changed.identity", "Wrong")), "Updates cannot change node identity");
            Check(first.Definition.Title == "Updated", "Rejected update preserves prior definition");
            first.Dispose(); first.Dispose();
            Check(!first.IsRegistered && registry.Nodes.Count == 1 && changes == 4, "Disposal unregisters exactly once");
            Throws<ObjectDisposedException>(() => first.Update(new TechNodeDefinition(first.Id, "Gone")), "Disposed handles cannot update registry");
            var replacement = registry.CreateTechNode(new TechNodeDefinition(first.Id, "Replacement"));
            first.Dispose();
            Check(replacement.IsRegistered && registry.Nodes.Count == 2, "Stale disposal cannot remove a replacement with reused identifier");
            Check(!registry.TryGet(null, out _) && !registry.TryGet("missing", out _), "Missing identifiers are safe to query");
            replacement.Dispose(); second.Dispose();
            Check(registry.Nodes.Count == 0, "Each mod can clean up its own registrations");
            Check(Chainloader.PluginInfos.ContainsKey("urakka.foundation.example"), "Separate example plugin loaded with a Foundation dependency");
            Check(FoundationApi.TechNodes.Nodes.Count(n => n.Id.StartsWith("urakka.foundation.example.")) == 3, "Separate example plugin registers native research before any game scene");
            Check(FoundationApi.TechNodes.Nodes.All(n => ReferenceEquals(((INativeTechNode)n).NativeNode, TechNode.Get(n.Id))), "Public handles refer to the actual game research catalog");
            Throws<ArgumentException>(() => FoundationApi.CreateTechNode(new TechNodeDefinition("mine", "Collision")), "API cannot replace a native game technology");
            Throws<ArgumentOutOfRangeException>(() => new TechNodeOptions(fixedCost: new CurrencyCollection(Currency.Gold, -1)), "Negative research costs rejected");
        }

    }
}

