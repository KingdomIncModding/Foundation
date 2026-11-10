using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Registers content with actual Kingdom Inc static registries. Registration is intentionally
    /// permanent until process exit: vanilla saves store string IDs and array indices.
    /// Register during BepInEx Awake, before the first kingdom is created or loaded.
    /// </summary>
    public interface IGameContentRegistry
    {
        IReadOnlyDictionary<string, string> Ownership { get; }
        ItemType RegisterItem(string modId, ItemType item);
        CraftingRecipe RegisterRecipe(string modId, CraftingRecipe recipe);
        Quest RegisterQuest(string modId, Quest quest);
        DungeonType RegisterDungeon(string modId, DungeonType dungeon);
        WarType RegisterWar(string modId, WarType war);
        /// <summary>Registers prestige.</summary>
        PrestigeNode RegisterPrestige(string modId, PrestigeNode node);
        ItemEnhancement RegisterEnhancement(string modId, ItemEnhancement enhancement);
    }
}
