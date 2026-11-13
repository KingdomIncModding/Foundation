using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class GameContentRegistry : IGameContentDirectory
    {
        readonly Dictionary<string, string> owned = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly List<string> registeredItems = new List<string>();
        internal IReadOnlyList<string> NamespacedItemIds => registeredItems;
        public IReadOnlyDictionary<string, string> Ownership => new Dictionary<string, string>(owned);
        public bool TryGetOwner(string kind, string nativeId, out string ownerId)
        {
            ownerId = null;
            return !string.IsNullOrWhiteSpace(kind) && !string.IsNullOrWhiteSpace(nativeId) &&
                owned.TryGetValue(kind + ":" + nativeId, out ownerId);
        }
        public IReadOnlyList<string> OwnedIds(string modId) =>
            Array.AsReadOnly(owned.Where(pair => pair.Value == modId)
                .Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        static void Check(string modId, string identifier, string type)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("Mod ID required.", nameof(modId));
            if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("Content ID required.", nameof(identifier));
            if (!identifier.StartsWith(modId + ":", StringComparison.Ordinal))
                throw new ArgumentException(type + " content IDs must be namespaced as '" + modId + ":name'.", nameof(identifier));
            // GamePlayer has fixed-length arrays for resources, prestige and research and
            // direct native JSON lookup for all other types. Late registration is unsafe.
            if (GamePlayer.Current != null) throw new InvalidOperationException("Register game content in plugin Awake before a kingdom exists.");
        }
        void Remember(string type, string id, string modId) => owned.Add(type + ":" + id, modId);
        public ItemType RegisterItem(string modId, ItemType item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            Check(modId, item.Identifier, "Item");
            if (ItemType.All.Any(e => e.Identifier == item.Identifier)) throw new ArgumentException("Duplicate item ID: " + item.Identifier);
            ItemType.Add(item);
            registeredItems.Add(item.Identifier);
            Remember("item", item.Identifier, modId);
            return item;
        }
        public CraftingRecipe RegisterRecipe(string modId, CraftingRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            Check(modId, recipe.Identifier, "Recipe");
            if (CraftingRecipe.All.Any(e => e.Identifier == recipe.Identifier)) throw new ArgumentException("Duplicate recipe ID: " + recipe.Identifier);
            if (recipe.Reagents == null || recipe.Results == null || recipe.Results.Length == 0)
                throw new ArgumentException("Native recipe needs a Reagents array and at least one result.");
            CraftingRecipe.Add(recipe);
            Remember("recipe", recipe.Identifier, modId);
            return recipe;
        }
        public Quest RegisterQuest(string modId, Quest quest)
        {
            if (quest == null) throw new ArgumentNullException(nameof(quest));
            Check(modId, quest.Identifier, "Quest");
            if (Quest.All.Any(e => e.Identifier == quest.Identifier)) throw new ArgumentException("Duplicate quest ID: " + quest.Identifier);
            if (quest.Challenges == null) throw new ArgumentException("Native quest requires a Challenges collection.");
            Quest.Add(quest);
            Remember("quest", quest.Identifier, modId);
            return quest;
        }
        public DungeonType RegisterDungeon(string modId, DungeonType dungeon)
        {
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            Check(modId, dungeon.Identifier, "Dungeon");
            if (DungeonType.All.Any(e => e.Identifier == dungeon.Identifier)) throw new ArgumentException("Duplicate dungeon ID: " + dungeon.Identifier);
            if (dungeon.Stages.Count == 0) throw new ArgumentException("Native dungeon needs at least one stage.");
            DungeonType.Add(dungeon);
            Remember("dungeon", dungeon.Identifier, modId);
            return dungeon;
        }
        public WarType RegisterWar(string modId, WarType war)
        {
            if (war == null) throw new ArgumentNullException(nameof(war));
            Check(modId, war.Identifier, "War");
            try { if (WarType.Get(war.Identifier) != null) throw new ArgumentException("Duplicate war ID: " + war.Identifier); }
            catch (KeyNotFoundException) { }
            if (war.Stages == null || war.Stages.Length == 0) throw new ArgumentException("Native war needs at least one stage.");
            WarType.Add(war);
            Remember("war", war.Identifier, modId);
            return war;
        }
        public ItemEnhancement RegisterEnhancement(string modId, ItemEnhancement enhancement)
        {
            if (enhancement == null) throw new ArgumentNullException(nameof(enhancement));
            // Native ItemEnhancementData.FromJson splits at the first colon to
            // separate ID from quality; a colon inside the ID cannot be loaded.
            // A dot is therefore used for namespaces in enhancement identifiers.
            if (string.IsNullOrWhiteSpace(modId) || !enhancement.Identifier.StartsWith(modId + ".", StringComparison.Ordinal)
                || enhancement.Identifier.Contains(":"))
                throw new ArgumentException("Enhancement IDs must be namespaced as 'modId.name' with no colon.");
            if (GamePlayer.Current != null) throw new InvalidOperationException("Register content in plugin Awake before loading a kingdom.");
            if (owned.ContainsKey("enhancement:" + enhancement.Identifier)) throw new ArgumentException("Duplicate enhancement ID: " + enhancement.Identifier);
            try { if (ItemEnhancement.Get(enhancement.Identifier) != null) throw new ArgumentException("Duplicate enhancement ID: " + enhancement.Identifier); }
            catch (KeyNotFoundException) { }
            ItemEnhancement.Add(enhancement);
            Remember("enhancement", enhancement.Identifier, modId);
            return enhancement;
        }
        public PrestigeNode RegisterPrestige(string modId, PrestigeNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Check(modId, node.Identifier, "Prestige");
            // Native PrestigeNode registers itself in its constructor; the content is
            // already present. Exposed here to record ownership/initialization order.
            if (!PrestigeNode.All.Contains(node)) throw new ArgumentException("PrestigeNode has not been registered by its constructor.");
            Remember("prestige", node.Identifier, modId);
            return node;
        }
    }
}
