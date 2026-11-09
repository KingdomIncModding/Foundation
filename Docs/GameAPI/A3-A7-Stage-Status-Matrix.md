# Stage A3–A7 status matrix (Foundation 0.10.5 source candidate)

Read with the controlling `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`.

| Stage | Native source areas | Foundation entry points (existing + extended) | Source status | Runtime status |
| --- | --- | --- | --- | --- |
| A3 — items, crafting, resources, economy | `ItemType`, `InventoryItem`, `EquipmentItemData`, `ItemEnhancement`, `CraftingRecipe`, `CraftingReagent`, `CraftingResult`, `GamePlayer`, `ForestSettings`, `FarmSettings` | `Items`, `Economy`, `Production`, `Content` (existing compatibility only), plus native machine recipes from Machine API | **Expanded**, static-contract checked; no custom content rewrite | **PENDING** full installed test |
| A4 — research, progression, adventures, combat | `TechNode`, `PrestigeNode`, `Quest`, `QuestMetadata`, `DungeonType`, `DungeonProgress`, `WarType`, `WarProgress`, `CombatantItemData`, `HumanItemData` | `Progression`, `Adventures`, `Combat`, native machine specializations | **Expanded**, source-confirmed; specialized machine actions remain dependent on A1 | **PENDING** live research, quest, dungeon, war, combat and equipment tests |
| A5 — workspace editing | `GamePlayer.Flows`, `FlowDiagram`, `FlowElement`, `FlowDeco`, `WorldFlowManager`, `WorldFlowElement`, `WorldFlowDeco` | `World`, `Workspace`, native A2 `IO` | **Expanded**, native manager used; unsupported batch/clone/undo explicitly deferred | **PENDING** live move + save/reload confirmation |
| A6 — UI/input/camera/audio/assets | `GameUI`, `PlayerInput`, `WorldFlowCamera`, `MusicManager`, `Translation`, `SpriteLibrary`, `SaveGame` | `UI`, `View`, `Audio`, `Presentation`, `Saves`, `Assets` (existing Foundation-owned registration frozen) | **Expanded** access to native controller, locales; no UI rewrite | **PENDING** actual menu, input, audio, save and asset tests |
| A7 — events/hooks/read-only values | `GamePlayer`, `WorldFlowManager`, `GameUI`, `HumanItemData`, `CombatantItemData`, native save, quest/war/dungeon and IO methods | Existing `Events` only as transport, game-origin `...Event` DTOs, `World`, `View`, `Progression`, A2 IO observers | **Expanded** native hook origins and native state parity; no new central event system | **PENDING** actual transition observations |

## Not equivalent to source completeness

The **Game API first** roadmap requires source API coverage *and* installed game behavior. Implementing additional native-backed methods cannot certify all game classes, all source signatures, all machine-specific interactions, or current-version Unity behavior. Native methods not described in the supplied original game source must be recorded as unsupported/unavailable, not implemented by guesswork. **A1 is not certified complete by this package.**

## Previous milestones preserved

- A2 Foundation 0.10.4 native IO/throughput remains unchanged; its static suite runs against this candidate version and must pass regression after install.
- Previous Core/Machine/UI implementation and their custom registries remain untouched, except the native bridge and direct API contracts listed in the reference. No Stage B rewrites.
- One production `Foundation.dll`. Separate `Foundation.NativeGameApiVerification.dll` is optional and development-only; remove after testing.

## Known gaps requiring live evidence

Native quest/reward/progression transitions, combat vitals and equipment interactions, UI-popup lifecycle, world save/reload, and music/input/camera are not exercised by file inspection. A1's specific recruitment/quest/dungeon machine operations are separate from A4 generic data access and must be confirmed against actual machine subclasses. The game has no general-purpose workflow cloning, undo transaction or generic batch-edit API exposed by the supplied source, so those custom utilities are deferred; they do not block the native API gate by themselves.
