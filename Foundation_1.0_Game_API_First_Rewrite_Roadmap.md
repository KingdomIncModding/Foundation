# Kingdom Inc. — Foundation Framework

## Foundation 1.0 Development Roadmap — Game API First, Custom Systems Rewrite Second

**Planning baseline:** Foundation 0.10.1 source/SDK  
**Release target:** Foundation 1.0  
**Last independently confirmed runtime audit (per the prior roadmap):** Foundation 0.9.3, 296 checks  
**Status of 0.10.x:** Changes are described as implemented in source, but equivalent installation/runtime confirmation is not established by the supplied roadmap.

> **Controlling decision:** Complete the **native Kingdom Inc. Game API** before rewriting Foundation-created systems. Keep the current custom implementations working as temporary infrastructure. Once native coverage is complete, rewrite or review the custom systems against that finalized Game API. Do not invent replacement gameplay systems.

This document **replaces** the priority order in the prior *Foundation 1.0 Remaining Development Roadmap*. It retains the prior workstreams and their reported statuses but reorganizes them around the agreed **Rewrite** inventory: **24 custom-system candidates (19 planned rewrites and 5 reviews)**. Their exact source-level presence and implementation status must be rechecked before modifications.

---

## 1. Scope and Boundaries

### 1.1 What belongs in the Game API

The Game API exposes **existing Kingdom Inc. behavior**: machines and their individual operations, FlowElement/FlowIO/FlowPipe, crafting, production, resources, workspace and editing, items, research, quests, dungeons, characters, combat, economy, input, audio, camera, assets, and native user-interface facilities. It must reflect real game objects, rules, data, state transitions, and restrictions.

A native API method may wrap an internal game call or provide a typed read-only view. It must **not** substitute a new simulation or silently change gameplay semantics. If the base game does not implement an action, mark it unavailable rather than presenting a new Foundation feature as “native.”

### 1.2 What belongs in the Rewrite

Foundation-created infrastructure belongs in the later **Rewrite** phase: mod registries, lifecycle/dependency/service management, inter-mod communication, Foundation event abstraction, signal registries/bindings, config UI, selectors and widgets, developer tooling, asset/content registration, custom persistence, and related compatibility machinery.

A mod-created item, machine, research node, signal, or UI panel is **not** a vanilla feature merely because Foundation connects it to a native system. The underlying native interface belongs in the Game API; the **registration and orchestration** of mod-created content belongs in the Rewrite.

### 1.3 Classification rules

| Classification | Meaning | When to work on it |
|---|---|---|
| **NATIVE** | Expose existing game classes, fields, operations, and UI behavior | **Stage A — now** |
| **BRIDGE** | Thin, necessary read/write wrapper over native behavior | **Stage A — now** |
| **REWRITE** | Foundation-created custom framework/service/UI capability | **Stage B — after Game API gate** |
| **REVIEW** | Existing custom implementation might be kept, rewritten, merged, or removed | **Stage B — after Game API gate** |
| **OUT OF SCOPE** | Entirely new gameplay rules or speculative systems | Not part of Foundation 1.0 Game API |

**Exception:** Fix any existing bootstrap, dependency, or compatibility defect that prevents building, loading, or testing the Game API. Such a fix is *maintenance*, not permission to redesign custom systems early.

---

## 2. Execution Order and Release Gates

| Order | Stage | Objective | Exit condition |
|---|---|---|---|
| **0** | Inventory and baseline | Check present source and expose native/custom boundaries | Feature inventory and clean baseline established |
| **A1** | Machines, production, and specialized operations | Expose every supported native machine individually | Native machine API matrix completed |
| **A2** | FlowIO, pipes, ports, and throughput | Complete native transport access | Read/edit/validate/observe paths verified |
| **A3** | Items, recipes, resources, economy | Expose native rules and state | Existing native item/economy operations covered |
| **A4** | Research, quests, dungeons, combat, progression | Expose native gameplay | Native gameplay APIs and state transitions covered |
| **A5** | Workspaces, placement, world editing | Expose native workspace operations | Supported operations documented and tested |
| **A6** | Native UI, input, camera, audio, and asset access | Expose existing game facilities | Native facilities discoverable; no new UI framework |
| **A7** | Native event hooks and read-only signals | Provide real game observations | Native hook/property coverage verified |
| **GATE A** | **Freeze Game API contracts** | Validate broad native coverage and behavior | **No planned Game API work remains undocumented or unverified** |
| **B1** | Core framework Rewrite | Rewrite mod lifecycle and service infrastructure | Core custom interfaces depend on Game API |
| **B2** | Registries/content/persistence Rewrite | Rewrite mod-owned content and saves | Native backing, ownership, recovery verified |
| **B3** | Signal Rewrite | Rewrite signal sources, selection, configuration | All signal flows verified against Game API |
| **B4** | UI Rewrite | Rewrite mod UI/components after native UI bridge | Native-themed controls and machine-specific panels |
| **B5** | Developer tooling Rewrite | Rewrite consoles, inspectors, diagnostics | Tooling uses Game API, not private gameplay access |
| **B6** | Five targeted reviews | Keep/rewrite/remove questionable infrastructure | Decisions and rationale recorded |
| **GATE B** | **Rewrite complete** | Retire duplicate paths and stabilize contracts | Full regression and compatibility pass |
| **1.0** | Release | Documentation, examples, packaging, migration | Stable distributable Foundation 1.0 |

**Dependency:** B1–B6 must not be implemented as a wholesale rewrite before **Gate A**. Documentation, API tests, and critical build fixes may proceed throughout.

---

# Stage 0 — Source Inventory and Baseline

1. Inventory the current Foundation source, installed binary, SDK, build scripts, tests, and existing mod consumers. Do not equate a source declaration with a working installed feature.
2. Inventory Kingdom Inc. native classes and ownership: machine subclasses, FlowElement/FlowIO/FlowPipe, workspace, research, crafting, economy, UI prefabs/controllers, event origins, and save handling.
3. For each public Foundation API, label it **NATIVE**, **BRIDGE**, **REWRITE**, or **REVIEW**. Record its actual game class/method backing and whether it mutates game state.
4. Mark old custom features **frozen**, except for bug fixes required to execute Game API tests.
5. Build with the actual game references and verify the game starts with Foundation enabled. Run current automated tests and record failures before editing.
6. Keep compatibility notes for existing mods; breaking changes to custom APIs belong in the later rewrite/migration phase, unless a native bridge is demonstrably incorrect.

**Baseline reference:** The previously supplied roadmap identifies approximately **65 flow-element-related source files**. Treat that as an audit lead, not as proof that all native behavior has been mapped.

---

# Stage A — Complete Kingdom Inc.'s Native Game API

## A1. Machine Framework — Native Machine Coverage

**Priority: A1 / critical.** Every supported machine must expose its **real machine-specific** input/output, configuration, runtime state, and native actions. Generic FlowElement access is insufficient when a subclass has special behavior.

### Crafting and Production

| Machines | Native Game API work |
|---|---|
| Alchemist, Blacksmith, Butcher | Recipe discovery, ingredients, production controls |
| Carpenter, Forge, Jeweler | Production and native upgrade interfaces |
| Kitchen, Leatherworker, Tailor | Native crafting configuration |
| Enchanter, Engraver, Scribe | Specialized native crafting behavior |
| Necromancer, Summoning Circle | Their particular native operations |
| Farm, Mine, Forest | Production, resources, native worker-related behavior |
| Adventurers Guild, Barracks, Supply Depot | Specialized native resource production |

### Logistics and Flow Control

| Machines | Native Game API work |
|---|---|
| Collector | Input selection and collection |
| Distributor | Output routing and configuration |
| Manifold | Input aggregation and native configuration |
| Filter | Native filter settings |
| Portal | Native channel/routing behavior |
| Armory | Storage and item selection |
| Stockpile | Storage behavior |
| Treasury, Hoard, Pantry, Library | Specialized storage operations |
| Incinerator | Native disposal configuration |
| Accountant | Native accounting configuration |

### Specialized Machines

| Machines | Native Game API work |
|---|---|
| Track Switch | Switch activation and configuration |
| Timer | Timing and activation controls |
| Appraiser | Comparison and selection operations |
| Inspector | Inspection operations |
| Refiner | Refinement, native failure probabilities and mana costs |
| Disenchanter | Disenchantment |
| Quest Board | Native quest selection and progression |
| Dungeon | Native dungeon selection and progression |
| Arena | Native combat configuration |
| Training Grounds | Native training |
| Inn | Native character/accommodation management |
| Mercenary Guild | Native recruitment |
| Requisition Office | Native contract management |
| War Effort | Native war configuration/progression |
| Shop, Luxury Shop, Restaurant | Native commercial operations |

**Definition of done:** For each relevant native type, document actual source class, exposed properties, actions, inputs, outputs, restrictions, lifecycle, and test coverage. Use dedicated controls where machine behavior differs. **Do not construct machine configuration panels yet**; that is Stage B4.

## A2. Native FlowIO, FlowPipe, Ports, and Throughput

| Feature | State in prior roadmap | Required native work |
|---|---|---|
| Inspect Input Ports | Implemented | Verify native mapping and edge cases |
| Inspect Output Ports | Implemented | Verify native mapping and edge cases |
| Connect Pipes | Implemented | Verify native connection contract |
| Disconnect Pipes | Implemented | Verify native disconnection contract |
| Observe Item Transfers | Implemented | Validate event timing and values |
| Throughput Measurement | Implemented | Ensure measured values reflect game, not a simulated capacity |
| Pipe Route Editing | Implemented in 0.10.1 source | Runtime-verification pending |
| Port Compatibility Discovery | Partial | Expose native port compatibility |
| Port Configuration | Partial | Expose real native settings |
| Automatic Connection Validation | Partial | Reuse real game validation |
| Machine-Specific IO Rules | Partial | Cover special machine ports and routing |
| Complete Connection Events | Partial | Observe native connect/disconnect operations |
| Flow Diagnostics | Partial | Expose native state; custom inspector UI is later |
| Batch Connection Operations | Missing | First check if the game supports equivalent operations; otherwise defer as a utility |

**Restriction:** Item-transfer pipes remain separate from Foundation's later signal system. No replacement transport, invented throughput caps, or privately maintained copy of the game's authoritative pipe graph.

## A3. Items, Crafting, Resources, Economy, and Production

The table below retains the prior status snapshot. **Native reads/operations** are Stage A; **registration of mod-created content and custom data** is deferred to B2.

| Feature | Prior state | Stage |
|---|---|---|
| Item Creation (native objects) | Implemented | A: verify |
| Item Serialization (native format) | Implemented | A: verify |
| Item Quality | Partial | A: complete native exposure |
| Item Enhancements | Partial | A: complete native exposure |
| Equipment | Partial | A: complete native exposure |
| Item Requirements | Partial | A: complete native exposure |
| Recipe Requirements | Partial | A: complete native exposure |
| Crafting Modifiers | Partial | A: complete native exposure |
| Item Metadata (native fields) | Partial | A: complete native exposure |
| Loot and Reward Integration (native rules) | Partial | A: complete native exposure |
| Item Registration (mod content) | Implemented in source | **B2: rewrite** |
| Recipe Registration (mod content) | Implemented in source | **B2: rewrite** |
| Custom Item Data | Partial | **B2: rewrite/standardize** |
| Missing Item Recovery | Partial | **B2/B6: review** |
| Item Icons and Visuals | Partial | A native lookup; B2 custom registration |

The earlier roadmap also calls out **economy and production** without a separate detailed coverage table. During Stage 0, create a concrete native method inventory for currencies, crafting costs, resource availability, production rates, and resource transactions; do not invent completion statuses for methods the original roadmap does not enumerate.

## A4. Research, Adventures, Combat, and Progression

### Research and Progression

| Feature | Prior state | Stage |
|---|---|---|
| Technology Tree Integration (native access) | Implemented | A: verify |
| Custom Requirements (native prerequisite behavior) | Partial | A: expose native rules; B2 custom definitions |
| Research Dependencies | Partial | A: native dependencies; B2 registration validation |
| Research Rewards | Partial | A: native reward behavior; B2 custom content mapping |
| Custom Unlocks | Partial | B2 after native unlock API |
| Prestige Integration | Partial | A: native access |
| Ascension Integration | Partial | A: native access |
| Nightmare Progression | Partial | A: native access |
| Research Tree Layout Management (native UI) | Partial | A: native behavior; B4 custom layout integration |
| Technology Registration | Implemented | **B2: rewrite registration interface** |
| Technology Removal Recovery | Partial | **B6: review** |
| Research UI Extensions | Partial | **B4: rewrite** |

### Quests, Dungeons, Wars, Characters, Combat

| Feature | Prior state | Stage |
|---|---|---|
| Quest Progression | Partial | A: native access |
| Dungeon Progression | Partial | A: native access |
| War Progression | Partial | A: native access |
| Combatant Access | Implemented | A: verify |
| Equipment Management | Partial | A: native access |
| Combat Statistics | Partial | A: native access |
| Damage and Healing | Implemented | A: verify native effects |
| Training | Partial | A: native access |
| Character Recruitment | Partial | A: native access |
| Rewards | Partial | A: native rules |
| Character Progression | Partial | A: native access |
| Quest Registration | Implemented in source | **B2: rewrite registration** |
| Dungeon Registration | Implemented in source | **B2: rewrite registration** |
| War Registration | Implemented in source | **B2: rewrite registration** |
| Adventure UI Integration | Partial | A native access; B4 custom interface |

**Definition of done:** Native state transitions, game rules, save behavior, and player-visible effects are exposed and tested without independently implementing quests, dungeons, combat, or research progression.

## A5. World and Workspace Editing

| Feature | Prior state | Required handling |
|---|---|---|
| Workspace Discovery | Implemented | Verify native mapping |
| Workspace Creation | Implemented | Verify native operation |
| Workspace Switching | Implemented | Verify native operation |
| Workspace Renaming | Implemented | Verify native operation |
| Machine Placement | Implemented | Verify native operation |
| Machine Removal | Implemented | Verify native operation |
| Machine Movement | Implemented | Verify native operation |
| Pipe Editing | Partial | Complete game-native editing |
| Decoration Placement | Implemented | Verify native operation |
| Decoration Removal | Implemented | Verify native operation |
| Workspace Cloning | Missing | Check for a native equivalent; otherwise defer |
| Multi-Machine Selection | Missing | Check for a native equivalent; otherwise defer |
| Batch Placement | Missing | Check for a native equivalent; otherwise defer |
| Batch Removal | Missing | Check for a native equivalent; otherwise defer |
| Machine Copy/Paste | Missing | Check for a native equivalent; otherwise defer |
| Workspace Templates | Missing | Do not introduce as native unless game provides them |
| Blueprint Integration | Missing | Check native APIs before adding any abstraction |
| Undo/Redo Integration | Missing | Check native undo/redo existence first |

**Important:** A utility built from several native operations is still a Foundation feature, **not** a missing native operation. Such utilities may be considered after Gate A but must not block native API completion merely because the prior roadmap called them “missing.”

## A6. Native UI, Input, Camera, Audio, and Assets

The aim of Stage A is to expose **existing game UI facilities**, not to build a second UI framework. Foundation's custom config widgets, machine panel registry, mod bar, and general developer UI are postponed to B4/B6.

**Native UI access:** Locate and expose the relevant game panel/menu creation paths, control types, callbacks, popup/notification behavior, tooltips, styling/theme resources, and machine-specific native configuration surfaces. Verify behavior within the game instead of duplicating its UI in a separate overlay.

**Native input/camera/audio:** Complete and verify player input access, camera state/control, sound playback and game audio service boundaries. Source-specific names and capabilities must come from the real Kingdom Inc. classes.

**Native asset access:** Verify real sprite, icon, prefab, AssetBundle, and localization access where present. Ownership, registration, custom asset dependencies, and cleanup policies remain B2.

### Prior asset/content status (split by ownership)

| Feature | Prior state | Stage |
|---|---|---|
| Sprite Loading | Implemented | A: verify game/Unity loader access |
| AssetBundle Loading | Implemented | A: verify loader access |
| Native Icon Registration | Implemented in source | A: native icon lookup; B2 custom registration |
| Localization | Partial | A: native localization; B2 mod strings |
| Custom Machine Assets | Partial | B2 |
| Prefab Registration | Partial | B2 |
| Asset Ownership | Partial | B2 |
| Asset Cleanup | Partial | B2 |
| Custom UI Assets | Partial | B2/B4 |
| Audio Asset Registration | Missing dedicated API | A: game audio exposure; B2 mod audio registration |
| Content Definition Validation | Partial | B2 |
| Content ID Management | Partial | B2 |
| Asset Dependency Management | Missing | B2 |
| Resource Caching | Partial | Review native cache first; B2 only where needed |

## A7. Native Events/Hooks and Read-Only Data Sources

The native Game API may expose real game notifications or callbacks and read-only snapshots. It must not first require a completed Foundation-wide event bus, signal routing layer, or custom inspector.

| Native event coverage | Prior state |
|---|---|
| Lifecycle Events | Implemented |
| Workspace Events | Partial |
| Machine Events | Partial |
| Pipe Events | Implemented |
| Item Transfer Events | Implemented |
| Currency Events | Implemented in source |
| Research Events | Implemented in source |
| Quest Events | Partial |
| Dungeon Events | Partial |
| War Events | Partial |
| Character Events | Partial |
| Inventory Events | Partial |
| UI Events | Partial |
| Input Events | Partial |
| Game Progression Events | Partial |

**Deferred to B1:** Centralized cross-mod subscriptions, filtering, priorities, cancellation, isolation, and event-bus lifecycle. Original statuses: **Event Priority — Missing**, **Cancellable Events — Missing**, **Event Filtering — Partial**. Any cancellation must only be offered where the native game can safely support it; do not fabricate cancellability.

### Native data later used by Foundation signals

| Game-owned read-only group | Recorded values |
|---|---|
| Player | Game time, ascensions, current workspace, nightmare state |
| Workspace | Machine count, pipe count, workspace name, decorations |
| Camera | Availability, zoom level, maximum zoom, zoom scale |

Native data access is Stage A. Their **`foundation:player`, `foundation:workspace`, `foundation:camera` signal-source registrations** are Stage B3. The read-only values remain owned by the game.

---

# Gate A — Game API Completion Criteria

Do not start the coordinated custom-system rewrite until all of the following are satisfied:

- [ ] Each supported native machine type is mapped to its actual game implementation and has appropriate property/operation/IO coverage or a documented reason an operation is inaccessible.
- [ ] FlowIO, FlowPipe, throughput, connection validation, and supported edit operations run through real native behavior.
- [ ] Item/crafting/economy, research/progression, adventures/combat, and workspace calls are covered to the extent the native game provides them.
- [ ] Native UI/input/camera/audio/asset APIs are documented and callable without constructing speculative replacement interfaces.
- [ ] Native event origins and supported read-only property sources are verified.
- [ ] API names, signatures, object ownership, threading/lifecycle, side effects, errors, and version constraints are documented.
- [ ] Build and runtime test results are recorded against the intended game version; earlier 0.9.3 checks are not represented as proof of 0.10.1 success.
- [ ] Existing Foundation-dependent mods still load, or required compatibility changes are explicitly recorded for the later migration plan.
- [ ] All “missing” items from the old roadmap are classified as **native feature**, **custom extension**, or **not present in game**. The Game API gate is not held hostage by features absent from the game.

**Gate A artifact:** An auditable Game API coverage matrix, reproducible test report, current public interface reference, and frozen baseline for the subsequent custom-system rewrite.

---

# Stage B — Rewrite Foundation-Created Systems

**Start only after Gate A.** The following **24 candidates** are the earlier agreed rewrite inventory, **not a claim that all 24 exist as completed code**. Audit each implementation before editing. “Rewrite” is a planned workstream; “Review” requires a keep/rewrite/merge/remove decision before coding.

## B1. Core Framework and Inter-Mod Infrastructure

| ID | Custom system | Decision | Rewrite objective / acceptance |
|---|---|---|---|
| R01 | Mod Registry | **Rewrite** | One authoritative mod registration and metadata directory; no duplicate registries |
| R02 | Mod Lifecycle Manager | **Rewrite** | Predictable load, ready, world load/unload, dispose behavior with safe cleanup |
| R03 | Dependency Manager | **Rewrite** | Declare, validate, and order dependencies with useful diagnostics |
| R04 | Service Registry | **Rewrite** | Typed, discoverable service registration scoped to providers and lifetimes |
| R06 | Event System | **Rewrite** | Deliver native-backed notifications to subscribers with ownership/error isolation |
| R07 | Inter-Mod Communication | **Rewrite** | Cross-mod discovery and shared interfaces without requiring direct references to consumer implementation DLLs |

**Prior-roadmap facts to preserve:** BepInEx Integration — Implemented; Mod Registration — Partial; Mod Dependencies — Partial; Mod-to-Mod Communication — Missing; Mod Lifecycle — Partial; Harmony Integration — Partial; Framework Services — Implemented; Error Isolation — Partial; Version Compatibility — Partial; Mod Unloading — Partial; Mod Metadata — Partial.

**Prior interoperability snapshot:** Mod Discovery — Partial; Mod Metadata — Partial; Dependency Registration — Partial; Cross-Mod Service Access — Missing; Shared Interfaces — Partial; Mod Compatibility Detection — Partial; Shared Content References — Missing standardized API; Mod Conflict Detection — Missing; API Version Negotiation — Missing; Dependency Load Ordering — Partial; Cross-Mod Events — Implemented; Optional Mod Integrations — Missing standardized API.

**Implementation order:** R01/R02 first, R03/R04 next, R06/R07 after service/lifecycle ownership is defined. Preserve native Game API behavior; Foundation supplies orchestration, not replacement game state.

## B2. Content Registries, Assets, and Persistence

| ID | Custom system | Decision | Rewrite objective / acceptance |
|---|---|---|---|
| R20 | Asset Registry | **Rewrite** | Register mod-owned assets using native/Unity loaders; explicit ownership and disposal |
| R21 | Content Registration SDK | **Rewrite** | Coherent registration of mod-created machines, items, recipes, upgrades, research, quests, dungeons, wars, and other supported content through native interfaces |
| R22 | Mod Persistence System | **Rewrite** | Mod-owned data persistence with stable IDs, safe loading, and lifecycle alignment; reuse native saves when applicable |

**Details retained from original workstreams:**

- **Items/recipes:** Existing source-level item/recipe registration must work with native quality, inventory, enhancements, crafting, and requirements. Do not implement independent crafting logic.
- **Research:** Technology registration/tree integration must use the native technology tree and research state. Custom requirements, unlocks, dependencies, rewards, and missing-node behavior must be based on real game semantics.
- **Adventure:** Quest/dungeon/war registration must use the native quest, dungeon, war, and character systems.
- **Assets:** Cover sprites, bundles, native icons, prefabs, UI assets, audio assets where supported, IDs, dependencies, localization, resource caching, cleanup and ownership. Do not imply unsupported game hooks exist.
- **Persistence:** First document which content already uses Kingdom Inc.'s own save format. Add custom persistence only for data the game does not save, with compatibility and removal behavior documented.

**Dependencies:** R20–R22 depend on Stage A content, asset, save, and lifecycle API coverage; R21 also depends on R01/R02 and R04.

## B3. Signal Infrastructure

| ID | Custom system | Decision | Rewrite objective / acceptance |
|---|---|---|---|
| R08 | Signal Registry | **Rewrite** | Register/discover stable sources, types, and exposed properties |
| R09 | Signal Source Selector | **Rewrite** | Find and choose sources with clear identity, search and filtering |
| R10 | Signal Property Selector | **Rewrite** | Select exposed source properties with data-type compatibility |
| R11 | Foundation Signal Adapters | **Rewrite** | Implement `foundation:player`, `foundation:workspace`, `foundation:camera` against verified native Game API values |

### Prior Signal System Status (retained)

| Feature | Prior state | Rewrite objective |
|---|---|---|
| Signal Source Registration | Implemented | Registry unification and native coverage |
| Property Exposure | Implemented | Complete game-backed property access |
| Source Selection | Implemented | Discovery, search/filtering |
| Property Selection | Partial | Type-aware selection |
| Signal Input Configuration | Partial | Standardize binding configuration |
| Signal Output Configuration | Partial | Standardize binding configuration |
| Signal Type Compatibility | Partial | Type matching/conversion, with explicit rules |
| Signal Change Events | Partial | Changes based on actual source value/event |
| Signal Persistence | Partial | Save bindings, restore safely, handle absent providers |
| Signal Diagnostics | Partial | Inspect live values/connections via developer tools |
| Signal Routing | Missing | Add reusable binding/routing only as a Foundation signal feature, not game transport |
| Signal Dependency Handling | Missing | Source/provider removal and safe unresolved state |

**Boundary:** Signal IO is not FlowPipe item transport. Do not infer signals from fictional engine features. Source selection and property selection remain distinct operations; signal input/output configuration is still required even when UI selection exists.

## B4. Mod Configuration and Custom UI

| ID | Custom system | Decision | Rewrite objective / acceptance |
|---|---|---|---|
| R12 | Mod Configuration UI | **Rewrite** | Native-themed mod configuration surfaces using the Game UI bridge |
| R13 | ModConfigTemplates | **Rewrite** | Reusable, consistent configuration templates |
| R15 | Custom UI Controls | **Rewrite** | Controls that genuinely need to exist beyond native game controls |

### Prior UI Status (retained)

| UI feature | Prior state |
|---|---|
| Native-Themed Panels | Partial |
| Buttons | Implemented |
| Toggles | Implemented |
| Sliders | Implemented |
| Dropdowns | Implemented |
| Color Swatches | Implemented in source |
| Signal Source Selectors | Implemented |
| Signal Property Selectors | Partial |
| Numeric Input Fields | Partial |
| Text Input Fields | Partial |
| Searchable Selection Lists | Partial |
| Tabs | Partial |
| Collapsible Sections | Partial |
| Tooltips | Partial |
| Popups | Partial |
| Confirmation Dialogs | Partial |
| Drag-and-Drop Controls | Missing |
| Reusable Layout Components | Partial |
| Machine-Specific Configuration Panels | Missing centralized system |
| Dynamic Property Editors | Partial |
| UI State Persistence | Partial |
| Live Telemetry Widgets | Partial |

### Native machine-specific UI registration

The prior roadmap requested a **centralized machine UI registry**. Treat that as a **B4 custom UI subfeature**, not a vanilla Game API requirement. On request, it should produce a machine-appropriate native-themed panel backed by the machine's **real** Game API: for instance, Track Switch → switch configuration, Timer → timing, Filter → filtering. Mod-created machines may register their own panels through the same extension point. The UI must not silently create new game behavior.

**Implementation order:** R12/R13 → R15 and machine-specific panels. R09/R10 selectors are developed in B3 but rendered through these UI contracts. Prefer native game controls first, custom controls only where native capabilities are inadequate.

## B5. Developer and Verification Tools

| ID | Custom system | Decision | Rewrite objective / acceptance |
|---|---|---|---|
| R16 | Foundation Developer Console | **Rewrite** | Game API-backed commands/visibility, isolated from gameplay internals |
| R17 | Foundation Inspector | **Rewrite** | Read native state via stable Game API snapshots; observe mod registry state separately |
| R18 | Foundation Diagnostics | **Rewrite** | Traceable, actionable health/error reporting across native bridges and mod-owned services |

### Prior SDK/tooling status (retained)

| Feature | Prior state | Handling |
|---|---|---|
| Example Research Mod | Implemented | Update after rewrite |
| Example Services Mod | Implemented | Update after rewrite |
| Example Gameplay Mod | Implemented in source | Update after rewrite |
| Developer Console | Implemented | R16 rewrite |
| Diagnostics | Implemented | R18 rewrite |
| Native API Bridge | Implemented in source | **Stage A**, not custom rewrite |
| API Documentation | Partial | Complete for Game API first; then rewrite APIs |
| Example Machine Mod | Missing | Add only after content SDK rewrite |
| Example UI Mod | Partial | Update after UI rewrite |
| Custom Content Templates | Missing | Add after content SDK rewrite |
| Mod Project Generator | Missing | Optional post-rewrite SDK task, not Game API |
| API Reference Documentation | Partial | Include verified public APIs |
| Developer Debug Panels | Partial | R16/R17/R18 |
| Compatibility Guidelines | Partial | Finalize for 1.0 |
| Migration Guides | Missing | Mandatory for replaced public contracts |

**Tooling restriction:** Verification should inspect actual game-state changes, rather than relying exclusively on self-reported framework checks.

## B6. Five Mandatory Review Decisions (Not Automatic Rewrites)

| ID | Custom system | Decision | Specific question and exit criterion |
|---|---|---|---|
| R05 | Harmony Patch Manager | **Review** | Can BepInEx/Harmony integration plus narrow Foundation ownership replace the current abstraction? Keep/rewrite/remove based on demonstrated need. |
| R14 | ModBarContext | **Review** | Is this a necessary adapter to native mod-bar UI, or redundant UI framework state? Identify native behavior before deciding. |
| R19 | Foundation Verification System | **Review** | Keep useful existing tests; rewrite infrastructure only if it blocks reproducible native/integration testing. |
| R23 | Missing Research Recovery | **Review** | Which removal scenarios are already handled safely by native research save/load behavior? Do not implement a second authoritative research store. |
| R24 | Workspace Node Registry | **Review** | Does it correctly register mod-created nodes through the real FlowElement, build-menu, IO, placement and save mechanisms? Keep/rewrite/merge after source verification. |

Each review must end with **KEEP**, **REWRITE**, **MERGE**, or **REMOVE**, supported by relevant source paths, Game API dependencies, consumer compatibility impact, and tests. Avoid deleting working compatibility behavior merely because an abstraction is custom.

---

## 3. Rewrite Inventory — Full 24-Item Checklist

| ID | Item | Decision | Scheduled stage | Status |
|---|---|---|---|---|
| R01 | Mod Registry | Rewrite | B1 | Not started; audit first |
| R02 | Mod Lifecycle Manager | Rewrite | B1 | Not started; audit first |
| R03 | Dependency Manager | Rewrite | B1 | Not started; audit first |
| R04 | Service Registry | Rewrite | B1 | Not started; audit first |
| R05 | Harmony Patch Manager | Review | B6 | Decision pending |
| R06 | Event System | Rewrite | B1 | Not started; audit first |
| R07 | Inter-Mod Communication | Rewrite | B1 | Not started; audit first |
| R08 | Signal Registry | Rewrite | B3 | Not started; audit first |
| R09 | Signal Source Selector | Rewrite | B3 | Not started; audit first |
| R10 | Signal Property Selector | Rewrite | B3 | Not started; audit first |
| R11 | Foundation Signal Adapters | Rewrite | B3 | Not started; audit first |
| R12 | Mod Configuration UI | Rewrite | B4 | Not started; audit first |
| R13 | ModConfigTemplates | Rewrite | B4 | Not started; audit first |
| R14 | ModBarContext | Review | B6 | Decision pending |
| R15 | Custom UI Controls | Rewrite | B4 | Not started; audit first |
| R16 | Foundation Developer Console | Rewrite | B5 | Not started; audit first |
| R17 | Foundation Inspector | Rewrite | B5 | Not started; audit first |
| R18 | Foundation Diagnostics | Rewrite | B5 | Not started; audit first |
| R19 | Foundation Verification System | Review | B6 | Decision pending |
| R20 | Asset Registry | Rewrite | B2 | Not started; audit first |
| R21 | Content Registration SDK | Rewrite | B2 | Not started; audit first |
| R22 | Mod Persistence System | Rewrite | B2 | Not started; audit first |
| R23 | Missing Research Recovery | Review | B6 | Decision pending |
| R24 | Workspace Node Registry | Review | B6 | Decision pending |

**Counts:** 19 Rewrite + 5 Review = 24 candidates. “Not started” describes the *new rewrite plan*, not whether the feature already exists in Foundation.

---

# Gate B — Rewrite Acceptance Criteria

- [ ] All R01–R24 items have a recorded audit result; each REVIEW item has an explicit keep/rewrite/merge/remove decision.
- [ ] All rewritten custom systems call the Game API where Kingdom Inc. provides the functionality; no duplicate simulation of native features remains.
- [ ] No Foundation system owns a second authoritative copy of native pipe, machine, item, research, combat, or workspace state.
- [ ] Custom registries use stable mod/content identifiers and handle ownership, collisions, disposal, reload and missing providers safely.
- [ ] Lifecycle, service discovery, dependencies, and inter-mod events are testable without mod-to-mod implementation coupling.
- [ ] Signal source/property selection, binding configuration, persistence, and removal behavior work without confusing signals with physical pipes.
- [ ] Native-themed mod UI works for machine-specific panels and mod-created content without adding fictional game controls.
- [ ] Developer console, inspector, and diagnostics rely on public/native-backed contracts and do not become required dependencies for gameplay.
- [ ] Existing research/workspace content and saved data load after the transition, or documented migration is provided where breaking changes are unavoidable.
- [ ] Tests include startup, world load/unload, saves, native operations, cross-mod dependencies, cleanup, error isolation, and existing mod consumers.

---

# Foundation 1.0 — Final Work

1. **Stabilize public contracts:** clear namespaces and interface boundaries for `Game API`, `Foundation framework`, `Mod SDK`, and developer tooling.
2. **Rebuild examples:** research, services, gameplay, native-machine integration, signal bindings, and native-themed UI, as supported by finalized APIs.
3. **Finish documentation:** machine coverage, API reference, extension/registration guides, lifecycle conventions, compatibility rules, examples and migration from pre-1.0.
4. **Run an actual installed runtime audit:** compile against correct references, install on the intended Kingdom Inc. version, verify logs, perform interactive native gameplay tests, and capture repeatable results.
5. **Ship a coherent package:** Foundation runtime, SDK reference files, examples and changelog. Do not claim a successful runtime audit merely because a build succeeds.

## P0–P3 Priority Mapping (Revised)

| Priority | When | Work |
|---|---|---|
| **P0 — now** | Stage 0 + A1–A7 | Verify baseline and complete native **Game API** coverage |
| **P0 gate** | Gate A | Prove Game API completeness within the game's actual feature set |
| **P1 — after Gate A** | B1 + B2 | Rewrite core framework, registries, content SDK, persistence |
| **P1 — after Gate A** | B3 + B4 | Rewrite signal infrastructure and custom mod UI |
| **P2 — after Gate A** | B5 + B6 | Rewrite developer tooling and decide five conditional reviews |
| **P3** | Gate B + 1.0 | Integration, compatibility, documentation, packaging and release |

## Final Development Rule

> **First expose what Kingdom Inc. already does. Then rewrite everything Foundation invented so that it uses those native APIs.**
>
> Do not begin the full Rewrite while the Game API is incomplete. Do not add invented gameplay to make the API appear complete. Do not confuse “available in source” with “verified working in game.”

---

## Document Lineage and Verification Notes

- **Primary source:** User-supplied *Foundation_1.0_Remaining_Development_Roadmap(1).md* (Foundation 0.10.1; originally 13 workstreams). Prior feature/state tables are preserved or split above, but scheduling has been changed by the user's explicit **Rewrite** decision.
- **Rewrite inventory:** The 24 Foundation-created features identified in the earlier discussion; 5 were marked **Review**, not automatic rewrites.
- **Additional SDK context available:** Foundation SDK 0.10.1 package and its XML/API examples. The SDK distinguishes custom registrations from native technology tree/workspace behavior; a full source-code implementation audit still requires current Foundation source rather than only the distributed runtime DLL.
- **No new implementation claim:** This roadmap is a planning document. It does not assert that edits, rewrites, installations, or runtime verification have been performed.
