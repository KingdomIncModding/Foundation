# Foundation

**A shared modding framework for Kingdom Inc.**

Foundation lets other mods extend *Kingdom Inc* through reusable game integrations. It provides native research and workspace registration, machine and logistics access, in-game mod settings, a shared Mod Bar, events, and services for mods that work together. The framework uses the game's existing systems wherever possible instead of replacing how the game plays.

The current source version is **0.12.0**. Foundation uses BepInEx 5 and ships its Core and Machines components together in **Foundation.dll**.

## Features

### Research and custom content

- A **Base Game / Mods** research view with a separate list and layout for each mod that registers technologies.
- Technology nodes, upgrade branches, research costs, prerequisites, and clickable links to required technologies.
- Custom workspace node types using the game's build menu, placement, connections, and save format.
- Registration support for compatible items, recipes, upgrades, quests, dungeons, and wars.
- Recovery handling for saved custom node types when their defining mod is unavailable; those nodes remain inert until their mod is restored.

### Machines and native logistics

- Machine discovery, inspection, available operations, and production information for supported game machines.
- Native input/output port inspection, connections, item transfers, and observed throughput.
- Machine-specific panels for supported types, including switches, buttons, timers, filters, crafting machines, and others.
- Native gameplay access for items, economy, research, quests, combat, and world/workspace data.

### In-game mod controls

- A shared **Mod Bar** that other mods can populate with buttons, toggles, dropdowns, and status text.
- An **Options** window with Game, Mods and Installed tabs, native bronze tab styling, and padded labels sized to their text.
- Additional top-level Options tabs supplied by participating mods, with shared Apply & Close and Cancel behavior.
- Vanilla settings remain available by default; a participating mod can hide individual rows while it supplies its own interface.
- Participating mods can also hide section headings independently while keeping the remaining settings available.
- Native-themed windows, controls, and panels that mods can reuse.

### Shared services

- Mod lifecycle and cross-mod service discovery.
- Game-backed events and separately registered signal sources and property bindings.
- Mod-owned data stored with the game's saves, with support for versioned migration.
- Loading helpers for mod-owned assets and game-provided UI resources.

The framework itself does **not** add demonstration buildings or technologies to your kingdom. Those appear when an installed mod registers them.

## Requirements

- **Kingdom Inc.** for PC (Unity Mono build).
- **BepInEx 5** with its bundled Harmony library.
- Mods using Foundation must be compatible with the installed Foundation version.

## Downloads

- **Foundation** is the runtime download required by mods that use this framework.
- **Foundation SDK** is an optional development download containing the matching framework library, IntelliSense documentation, guides, and example projects. Extract it into a development folder; install the Foundation runtime separately in the game.

## Installation

1. Install BepInEx 5 for Kingdom Inc.
2. Close the game.
3. Install the Foundation player package so that the runtime file is at `BepInEx/plugins/Foundation/Foundation.dll` in your Kingdom Inc game folder.
4. Start the game, then install or enable mods that depend on Foundation.

If updating, close the game before replacing Foundation. Back up existing kingdoms before changing framework versions or removing mods that supply saved content. No separate `Foundation.Machines.dll` is needed. Mods built against that older separate assembly must be rebuilt for the unified runtime.

## Using Foundation

Open the game's research tree to access **Base Game** and **Mods** categories when mods register research. Open **Options** to access settings registered by installed mods. The Mod Bar displays controls supplied by participating mods; it can be empty when no mod adds controls.

If a saved kingdom contains custom buildings from a mod you remove, reinstall that mod to restore the original behavior of those buildings. Foundation can preserve missing node data as placeholders, but a missing mod's gameplay functionality is not available.

## Compatibility

Foundation uses Harmony hooks to integrate with native game methods. These are the hooks registered during normal gameplay by this source version. Their effects are listed below; compatibility with any particular third-party mod depends on that mod's changes to the same methods.

| Class.Method | Hook Type | Impact |
|---|---|---|
| TechTreeMenu.OnEnable | Postfix | Adds the Base Game/Mods research navigation. |
| TechTreeMenu.OnDisable | Prefix | Closes the mod research navigation. |
| TechTreeMenu.ShowTechTree | Prefix / Postfix / Transpiler | Selects the right research category and displays its registered nodes. |
| TechTreeMenu.UpdateTechTree | Postfix | Lays out registered research when the Mods category is open. |
| TechTreeMenu.Update | Prefix | Keeps Mods research camera movement separate from the base tree. |
| TechTreeMenu.FocusNode | Prefix | Focuses research in the correct category, including custom root nodes. |
| TechTreeListView._addNodes.MoveNext | Transpiler | Filters research listings for the selected category. |
| UIElementTechNode.UpdateNodeStatus | Postfix | Makes displayed research prerequisites navigable. |
| WorldFlowCamera.RecalculateBounds | Postfix | Allows camera movement in empty mod research categories. |
| TechNode.ResetCost | Postfix | Restores configured fixed costs on registered research. |
| SpriteLibrary.Get | Prefix | Resolves registered custom research icons. |
| GamePlayer.HasTech | Prefix | Keeps native research storage sized for registered technologies. |
| GamePlayer.ToJson | Postfix | Omits mod-only research notification counters from saves. |
| FlowElement.GetAllTypes | Postfix | Includes registered custom workspace node types in the build catalog. |
| FlowElement.Create | Prefix | Creates registered node types and placeholders for missing saved types. |
| FlowElement.FromJson | Prefix / Finalizer | Loads custom workspace nodes or retains missing types as placeholders. |
| FlowElement.ToJson | Postfix | Preserves saved port data for missing workspace node types. |
| FlowPrefabManager.Get | Prefix | Supplies game-style previews for registered or missing custom node types. |
| FlowUpgrade.Create | Prefix | Creates registered custom upgrade types when requested. |
| InventoryItem.FromTypeString | Prefix | Restores registered namespaced items from saved strings. |
| WorldFlowManager.CreateNewElement | Postfix | Notifies mods when a native workspace element is placed. |
| WorldFlowManager.RemoveElement | Prefix / Postfix | Notifies mods when a native workspace element is removed. |
| WorldFlowManager.ShowFlow | Prefix / Postfix | Notifies mods when the selected workspace changes. |
| WorldFlowElement.UpdatePosition | Prefix / Postfix | Notifies mods when a workspace element moves. |
| FlowPipe.Connect | Postfix | Reports successful native pipe connections. |
| FlowDiagram.DisconnectPipe | Prefix / Postfix | Reports successful native pipe disconnections. |
| WorldFlowManager.CreateNewPipe | Postfix | Reports completed native pipe placements. |
| WorldFlowPipe.ItemTransferred | Postfix | Reports item transfers and records observed throughput. |
| FlowPipe.Update | Prefix | Pauses item flow through placeholders for missing node types. |
| GameUI.Start | Postfix | Attaches the shared Mod Bar to the game interface. |
| GameUI.ToggleUI | Postfix | Closes Mod Bar popups when the HUD is hidden. |
| GameUI.ProcessEscape | Prefix | Lets Escape close Mod Bar popups before other game menus. |
| GameUI.TogglePopupMenu | Prefix / Postfix | Reports native popup visibility changes. |
| OptionsUI.Start | Postfix | Attaches mod settings to the game Options menu. |
| OptionsUI.OnEnable | Postfix | Refreshes mod settings when the Options menu opens. |
| OptionsUI.ApplyAndClose | Prefix | Applies pending mod settings with game options. |
| OptionsUI.CancelOptions | Prefix | Discards pending mod settings when options are canceled. |
| SaveGame.SaveCurrentState | Postfix | Embeds mod-owned data in native saves and emits save lifecycle events. |
| SaveGame.LoadState | Prefix / Postfix / Finalizer | Restores mod data, handles missing research notices, and emits load events. |
| SaveGame.Store | Postfix | Reports completion of native save storage. |
| GamePlayer.CreateNewPlayer | Postfix | Initializes mod-owned data for a new kingdom. |
| GameManager.Start | Postfix | Notifies mods when the game world is ready. |
| GameManager.OnApplicationQuit | Prefix | Notifies mods before the game world closes. |
| RecipeElement.SetupRecipe | Prefix / Postfix | Reports changes to a machine’s selected recipe. |
| Filter.SetFilterType | Prefix / Postfix | Reports changes to a native filter’s type. |
| FlowElement.SetUpgrade | Prefix / Postfix | Reports changes to a machine’s native upgrade. |
| QuestBoard.UpdateQuest | Prefix / Postfix | Reports selected quest changes. |
| Dungeon.SetDungeonType | Prefix / Postfix | Reports selected dungeon changes. |
| InventoryItem.SetQuality | Prefix / Postfix | Reports changes to item quality. |
| GamePlayer.AddCurrency | Prefix / Postfix | Reports currency gained through the native game. |
| GamePlayer.SpendCurrency | Prefix / Postfix | Reports currency spending through the native game. |
| GamePlayer.AddResource | Prefix / Postfix | Reports changes to native resources. |
| GamePlayer.SetFarmResource | Prefix / Postfix | Reports changes to farm resources. |
| GamePlayer.AddTech | Prefix / Postfix | Reports native research unlocks. |
| GamePlayer.UnlockDungeon | Prefix / Postfix | Reports newly unlocked dungeons. |
| GamePlayer.UnlockWar | Prefix / Postfix | Reports newly unlocked wars. |
| GamePlayer.SetPrestigePoints | Prefix / Postfix | Reports changes to prestige points. |
| GamePlayer.DoAscension | Postfix | Reports completed native ascensions. |
| QuestProgress.QuestCompleted | Postfix | Reports native quest completion. |
| DungeonProgress.DungeonTick | Prefix / Postfix | Reports dungeon stage changes. |
| WarProgress.UpdateWar | Prefix / Postfix | Reports war stage changes. |
| HumanItemData.AddEquipment | Prefix / Postfix | Reports native equipment additions. |
| HumanItemData.RemoveEquipment | Prefix / Postfix | Reports native equipment removals. |
| CombatantItemData.TakeDamage | Prefix / Postfix | Reports changes to combatant health or energy after damage. |
| CombatantItemData.Heal | Prefix / Postfix | Reports changes to combatant health or energy after healing. |
| CombatantItemData.LoseEnergy | Prefix / Postfix | Reports changes to combatant energy. |

Foundation is a community-made modding framework, not an official component of Kingdom Inc.
