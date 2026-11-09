# Foundation 0.10.5 — Stage A3–A7 native Game API source candidate

**Controlling plan:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md` (Stage 0, A1–A7, Gate A, then B1–B6).  This is **one complete source tree**, not a partial overlay. There are no Stage B custom-system rewrites and no second production DLL. A2 0.10.4 remains intact; A1 type-by-type completion is a separate prerequisite and is not certified here.

**Authoritative baseline:** `Foundation-0.10.4-Native-Machine-IO-Throughput-Complete-Source.zip` (itself built on the user's `Foundation-Source-20261109-094126-36516.zip`) and the user-provided `KingdomIncSrc.zip`. The native source ZIP represents decompiled game code supplied for the project, not a verified match to the installed game assembly until the Windows tester compares/builds against it.

**Completion language:** All five Stage A3–A7 categories have existing native-backed Foundation services and additional typed source-backed calls in this source package. This is **source-implemented**, **not 100% installed/runtime-verified**. Neither code inspection nor an offline static test can make a Unity/BepInEx runtime claim. Unexercised event origins will remain `PENDING` in the independent test report. Do not check the roadmap's Gate A exit boxes based only on this archive.

## Stage A3 — Items, recipes, resources, economy, production

### Native item/recipe APIs

`FoundationApi.Items` (`IGameItemsService`) continues exposing the native `ItemType.All` and `CraftingRecipe.All` catalogs, lookup, native item construction/serialization/cloning, native input acceptance, and producer/consumer queries. This version adds:

| Call | Original game implementation / native rule | Semantics |
| --- | --- | --- |
| `Enhancements`, `FindEnhancement(id)` | `ItemEnhancement._allEnhancements` (actual game-owned private dictionary) | Read-only snapshot/list or exact native definition; **not** a second custom registry. |
| `EquipmentEnhancements(item)`, `EquipmentStat(item, stat)` | `EquipmentItemData.Enhancements`, `GetStat` | Live native item state; empty/zero for non-equipment. |
| `IsUnlocked(type)`, `NativeBaseValue(type)` | `ItemType.IsUnlocked`, `BaseValue` | Native unlock and production-dependent value rules; no synthetic unlock or prices. |
| `NativeSellValue(item)`, `AppraiserStat(item, stat)`, `CanEnhance(item, type)` | `InventoryItem.SellValue`, `GetAppraiserStat`, `CanEnhance` | Native quality/enhancement/equipment restrictions. |
| `RecipeAvailable(recipe)`, `RecipeCost`, `RecipeReward` | `CraftingRecipe.IsAvailable`, `CraftingCost`, `CraftingReward` | Uses the actual required tech and currency definitions. Returned currency objects are native references; don't edit the fields. |
| `RecipeReagents`, `RecipeResults`, `RequiredQuantity`, `ProducedQuantity` | `CraftingRecipe.Reagents`, `Results`, `GetReagentCount`, `GetResultCount` | Returns a read-only array view/copy of native definitions. Filter-based reagents remain native delegates, not forced into a type-only requirement. |
| `NativeItemSources(type)` | `ItemType.GetItemSources` | Native quest/dungeon/crafting source descriptors. |

`FoundationApi.Economy` adds `CanAfford(Currency, long)`, `ResourceCostMultiplier(ResourceItemType)`, `AdditionalWorkspaceCost()` delegated to `GamePlayer.CanAfford`, `GetResourceCostMultiplier`, and `GetExtraWorkflowCost`. Existing wallet, transactions, resource quota/demand, exchange tax and upgrade-price services remain unchanged. No parallel currency state or independently calculated exchange rate is created.

`FoundationApi.Production` adds native forest worker total, blocked states, actual utilization/quota multiplier, resource unlock-state reads, next-worker price and farm plot price. Its existing `TryHireForestWorker` now refunds the actual native cost if native `ForestSettings.AddWorkerCount` rejects the hire; it never substitutes another workforce algorithm. Farm/forest expansion/edit operations still use native `FarmSettings`/`ForestSettings` and `GamePlayer` payment calls.

**No invention:** custom recipe registration, custom asset registration and Foundation-owned save state remain frozen until Stage B2. Existing registration surfaces are preserved for compatibility but not rewritten here.

## Stage A4 — Research, quests, dungeons, wars, combat, progression

`FoundationApi.Progression` now exposes research prerequisites, available child tech, native availability/locked state and cost/cost tier through `TechNode.AllRequirements`, `AvailableTech`, `IsAvailable`, `IsLocked`, `GetCost`, `CostTier`. Existing unlock, prestige, ascension, nightmare and upgrade controls continue routing to actual `GamePlayer` operations.

`FoundationApi.Adventures` adds source-backed quest duration, first/repeat rewards, unlocked state, native dungeon availability and supply cost, native dungeon stage list, and war-stage state. It returns the game's `Quest`, `QuestReward`, `DungeonType`, `DungeonStage` and `WarType` objects, never Foundation-generated quests.

`FoundationApi.Combat` now exposes `GetCombatPower`, `GetBaseDamage`, `GetDamageReduction`, `HasTrait`, `GetTraits(false)`, and native equipment compatibility/conflict/equipped-item reads. Existing `Heal`, `Damage`, `SpendEnergy`, `Train` already call native `CombatantItemData` methods and are retained as-is. Combat progression is not simulated by the bridge.

**Specialized machine ownership:** Quest Boards, Dungeons, Arenas, Trainers, Inns, Mercenary Guilds etc. still require full A1 type-specific mapping and actual kingdom tests. The A4 typed data facade does not prove every such machine action was tested. No new quest/dungeon/war rule engine is introduced.

## Stage A5 — Workspaces and editing

`FoundationApi.World` adds `ActiveFlowIndex`, native machine/pipe/decoration counts and snapshot of elements for a given `FlowDiagram`. Its existing `FindElement`, `FindElements`, `SelectFlow`, `TryBuyFlow`, `RenameFlow`, `SetCamera` use the actual `GamePlayer`/`WorldFlowManager` and retain existing semantics.

`FoundationApi.Workspace` adds `MoveToFlow(FlowElement, int)` and `MoveDecorationToFlow(FlowDeco, int)`. Both verify real installed kingdom ownership, valid destination index and live native world-view objects, then delegate to **`WorldFlowManager.MoveToFlow`**. A disconnected machine can be moved; connected machines are deliberately rejected to avoid orphaning the native pipe graph. The service does not maintain a second world model. Existing purchase, placement, deletion, movement, upgrade and decoration APIs remain.

**Not vanilla** (deferred, not fabricated): automatic workspace duplication, a batch/undo framework, generic blueprint transaction API, and arbitrary clipboard/scripting automation where the original game has no native operation. Existing UI-specific area copy/move features are not evidence of a safe generic Game API method.

## Stage A6 — Native UI, input, camera, audio, assets

`FoundationApi.UI.NativeUi` provides the actual `GameUI.Instance`, including its named popup references, native UI state, and callbacks. `FoundationApi.View.NativeInput` and `.NativeCamera` expose the real `PlayerInput.Instance` and `WorldFlowCamera.Instance`, as well as the existing `NativeActionAsset`. `FoundationApi.Presentation.NativeLocales` and `TranslateOnly` call the game's `Translation.All` and `Translation.TranslateOnly`; the original `SpriteLibrary` lookup/catalog remains in the service. `FoundationApi.Audio` uses `MusicManager.Play/Queue/PreviewVolume` without a second mixer. `FoundationApi.Saves` continues using `SaveGame.SaveCurrentState` and native save/restore formats.

The existing custom Options config widgets, draggable windows, machine panel registrations and asset registration infrastructure are **not rewritten**. They are Stage B4/B2 features and remain functioning as previously implemented. Vanilla art from the user's immutable `Assets.zip` is not embedded or redistributed.

## Stage A7 — Native events and read-only values

Stage A7 needs game-origin observation, not a new Foundation-wide event bus. Existing currency/resource/research/quest/dungeon/war/quality/prestige/ascension, machine, pipe, world lifecycle and save hooks remain. This source adds **post-commit observation** on exact original methods:

| Typed event | Native Harmony origin | Actual event payload |
| --- | --- | --- |
| `NativeWorkspaceSelectedEvent` | `WorldFlowManager.ShowFlow(int)` | Old and new native workspace indices plus `FlowDiagram`. |
| `NativePopupVisibilityEvent` | `GameUI.TogglePopupMenu(UIPopupMenu,bool,bool)` | Native popup visibility, including a previous popup closed due to replacement. |
| `NativeEquipmentChangedEvent` | `HumanItemData.AddEquipment`, `RemoveEquipment` | Real added/removed `InventoryItem`; reports conflict removals during AddEquipment as well. |
| `NativeCombatantVitalsChangedEvent` | `CombatantItemData.TakeDamage`, `Heal`, `LoseEnergy` | Native before/after `Health01` and `Energy01` without a second health model. |

These event wrappers are read-only notifications delivered through the **existing** Foundation event service. No event makes gameplay cancellable, and no signal registration/routing is introduced. `FoundationApi.Native` remains a public bridge for source-confirmed public game members without guessed data models. `FoundationApi.World`, `FoundationApi.View`, `FoundationApi.Progression` etc. supply native read-only values that later Stage B3 signal adapters can consume.

**Known hook coverage limitation:** Some native events cannot be observed until actual play triggers their native origin. The report leaves such checks PENDING rather than inventing an event. Input actions can be subscribed to directly through the native `InputActionAsset` (no duplicate custom input pipeline). Unknown game-specific callbacks are **not** assumed to exist.

## Thread safety, references, side effects

- Native object reads and calls are Unity-game-thread operations; do not call the game object model from arbitrary worker threads. The existing A2 IO facade explicitly checks main thread; some legacy high-level gameplay services do not enforce it, so consumers remain responsible for game-thread dispatch.
- Returned `FlowDiagram`, `ItemType`, `CurrencyCollection`, `GameUI`, `PlayerInput`, `CombatantItemData`, etc. are **live native objects**; retain them only while their game/world owner exists, and do not mutate returned collections or readonly definition state behind Foundation's back.
- `IReadOnlyList<T>` is a snapshot for catalog/selection queries, not ownership of a native object.
- Native mutators may trigger achievements, autosaves, native events, and game rules. Use test kingdoms; the independent verification tool runs read-only by default and only moves/restores a disconnected native machine if the opt-in flag is supplied.
- `ItemEnhancement._allEnhancements`, like existing Foundation `Translation._lines`/`SpriteLibrary.Sprites`, is a **pinned private engine field**. Its existence is verified against `KingdomIncSrc.zip` and should be rechecked when the game updates. The game may change internal types between releases.
- Missing game features return no invented counterpart. Preserve old public APIs until the later compatibility-migration stage.

## Source tests and independent runtime verification

Run offline with extracted game source:

```text
python Tests\verify_game_api_a3_a7.py --game-source <extracted KingdomIncSrc\Source> --report Docs\GameAPI\A3-A7-Static-Source-Report.json
python Tests\verify_native_io_a2.py --game-source <extracted KingdomIncSrc\Source>
python Tests\verify_ui_source.py
python Tests\verify_source.py
```

**On installed Windows game, after backing up:**

```text
Foundation\Tests\NativeGameApi\INSTALL-A3-A7-TEST.cmd
```

This performs the unified full Foundation build/install against installed real game DLLs, then compiles a **development-only** `Foundation.NativeGameApiVerification.dll` from those references. Launch the game and load a **disposable** kingdom with crafting, at least one research tree, farm/forest if unlocked, equipment/characters, at least two workspaces, and access to native menus and adventure content. Switch workspaces, open/close an Options popup, acquire/spend currency, equip an item, and progress quests/dungeons/wars. For the reversible cross-flow machine move test, optionally start the disposable kingdom with `-foundation-gameapi-mutate` and have a disconnected machine plus two flows.

After game exit:

```text
Foundation\Tests\NativeGameApi\GAME-API-TEST.cmd
```

Choose **3 Read Report**. The report records **PASS**, **FAIL**, **PENDING**, installed Foundation DLL SHA256, assembly version and source game assembly SHA256. It rejects stale reports or builds with mismatched Foundation hashes. Any PENDING means its particular native behavior was *not observed*, not that the API is inherently broken. Remove verifier (menu option 4) after testing; keep only `Foundation.dll` in production.

**Release gate:** A3–A7 cannot honestly be marked **100% runtime verified** until the newly built DLL and independent runtime scenarios pass on the intended installed Kingdom Inc version, with no unclassified unsupported features. Stage A1 and Gate A still have their own completion requirements. This is a consolidated single-package source implementation with a reproducible verification pathway, not a retrospective claim that the runtime checks happened here.
