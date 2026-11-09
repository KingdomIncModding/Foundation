# Kingdom Inc. — Foundation Framework

## Remaining Development Roadmap

**Current development version:** Foundation 0.10.1  
**Target:** Foundation 1.0 — Complete Modding Framework

Foundation already has a substantial base. The remaining work is primarily about **completing game-system coverage, standardizing the APIs, and making the framework capable of supporting virtually any type of Kingdom Inc. mod**.

The previously supplied game source includes **65 files describing flow-element classes**, including machines, base classes, development elements, and specialized slots.

The objective is to expose the game's actual functionality through Foundation rather than create replacement systems.

> **Release status:** Foundation 0.9.3 was the last version with a confirmed successful runtime audit, including 296 checks. The newer 0.10.x expansions are implemented in source but have not received the same confirmation.

---

## 1. Core Framework

**Priority: CRITICAL**

| Feature | Current State | Remaining Work |
|---|---|---|
| BepInEx Integration | Implemented | Finalize plugin lifecycle integration |
| Mod Registration | Partial | Centralized registry for all mod content |
| Mod Dependencies | Partial | Dependency resolution and load ordering |
| Mod-to-Mod Communication | Missing | Shared service registration and discovery |
| Mod Lifecycle | Partial | Complete initialization, world loading, unloading, and cleanup handling |
| Harmony Integration | Partial | Shared patch management and conflict handling |
| Framework Services | Implemented | Standardize service interfaces |
| Error Isolation | Partial | Prevent individual mod failures from interrupting other mods |
| Version Compatibility | Partial | API versioning and compatibility guarantees |
| Mod Unloading | Partial | Distinguish unloadable services from permanent game registrations |
| Mod Metadata | Partial | Consistent name, author, version, description, and dependencies |

### Required Outcome

A mod should be able to register itself, request Foundation services, access game functionality, and communicate with other mods without implementing its own infrastructure.

---

## 2. Machine Framework

**Priority: CRITICAL**

This is one of the largest remaining tasks.

Foundation currently exposes general machine operations and some specialized controls. However, **not every machine has a complete API exposing its individual functionality**.

### Crafting and Production

| Machines | Remaining |
|---|---|
| Alchemist, Blacksmith, Butcher | Complete recipe, ingredient, and production controls |
| Carpenter, Forge, Jeweler | Complete production and upgrade interfaces |
| Kitchen, Leatherworker, Tailor | Complete crafting configuration |
| Enchanter, Engraver, Scribe | Expose specialized crafting mechanics |
| Necromancer, Summoning Circle | Expose their specific native operations |
| Farm, Mine, Forest | Complete production, resource, and worker controls |
| Adventurers Guild, Barracks, Supply Depot | Expose their specialized resource production |

### Logistics and Flow Control

| Machines | Remaining |
|---|---|
| Collector | Complete input selection and collection controls |
| Distributor | Output routing and configuration |
| Manifold | Input aggregation and configuration |
| Filter | Complete native filtering options |
| Portal | Channel and routing configuration |
| Armory | Storage and item selection |
| Stockpile | Complete storage behavior |
| Treasury, Hoard, Pantry, Library | Specialized storage controls |
| Incinerator | Disposal configuration |
| Accountant | Native accounting configuration |

### Specialized Machines

| Machines | Remaining |
|---|---|
| Track Switch | Complete switch behavior and configuration |
| Timer | Complete timing and activation controls |
| Appraiser | Complete comparison and selection controls |
| Inspector | Inspection operations |
| Refiner | Complete refinement controls |
| Disenchanter | Disenchantment operations |
| Quest Board | Quest selection and progression |
| Dungeon | Dungeon selection and progression |
| Arena | Combat configuration |
| Training Grounds | Character training |
| Inn | Character and accommodation management |
| Mercenary Guild | Native recruitment functionality |
| Requisition Office | Contract management |
| War Effort | War configuration and progression |
| Shop, Luxury Shop, Restaurant | Specialized commercial operations |

**Completion requirement:** Every supported machine should expose its relevant native properties, operations, inputs, outputs, and configuration options.

This includes machine-specific controls rather than forcing every machine into the same generic interface.

---

## 3. Signal System

**Priority: CRITICAL**

The existing signal registry is a useful starting point. What's still needed is a complete way for mods to discover, expose, configure, and connect signals.

| Feature | State | Remaining |
|---|---|---|
| Signal Source Registration | Implemented | Expand native coverage |
| Property Exposure | Implemented | Support additional game properties |
| Source Selection | Implemented | Improve search, filtering, and discovery |
| Property Selection | Partial | Type-aware property filtering |
| Signal Input Configuration | Partial | Standardized input configuration |
| Signal Output Configuration | Partial | Standardized output configuration |
| Signal Type Compatibility | Partial | Consistent type matching and conversion |
| Signal Change Events | Partial | Standardize notification behavior |
| Signal Persistence | Partial | Save bindings and restore missing sources |
| Signal Diagnostics | Partial | Inspect live values and connections |
| Signal Routing | Missing | Reusable routing and binding infrastructure |
| Signal Dependency Handling | Missing | Handle unavailable sources safely |

### Important Distinction

Signals should remain separate from the game's physical item-transfer pipes.

Foundation should provide the APIs needed to interact with both systems without inventing a replacement transport simulation.

---

## 4. Native IO and Throughput

**Priority: HIGH**

Foundation already exposes several pipe operations. The remaining work involves completing the editing and inspection APIs.

| Feature | State |
|---|---|
| Inspect Input Ports | Implemented |
| Inspect Output Ports | Implemented |
| Connect Pipes | Implemented |
| Disconnect Pipes | Implemented |
| Observe Item Transfers | Implemented |
| Throughput Measurement | Implemented |
| Pipe Route Editing | Implemented in 0.10.1 source |
| Port Compatibility Discovery | Partial |
| Port Configuration | Partial |
| Automatic Connection Validation | Partial |
| Machine-Specific IO Rules | Partial |
| Complete Connection Events | Partial |
| Flow Diagnostics | Partial |
| Batch Connection Operations | Missing |

**Required outcome:** A mod should be able to interact with native IO without having to directly manipulate internal pipe collections.

---

## 5. UI Framework

**Priority: CRITICAL**

This is another major remaining area.

Foundation has configuration controls and native menu access, but it doesn't yet provide a complete reusable UI framework for mod developers.

| Feature | State |
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

### The Biggest Missing Component

**A centralized machine UI registry.**

For example, a mod should be able to request the configuration UI for a specific machine type.

Foundation would then create the appropriate panel using that machine's native functionality.

A Switch would expose switch operations, a Timer would expose timing controls, and a Filter would expose filtering options.

All would share the game's existing visual theme.

This also needs to support mod-created machines.

---

## 6. Item and Crafting Framework

**Priority: HIGH**

| Feature | State |
|---|---|
| Item Registration | Implemented in source |
| Recipe Registration | Implemented in source |
| Item Creation | Implemented |
| Item Serialization | Implemented |
| Item Quality | Partial |
| Item Enhancements | Partial |
| Equipment | Partial |
| Item Requirements | Partial |
| Recipe Requirements | Partial |
| Crafting Modifiers | Partial |
| Item Metadata | Partial |
| Custom Item Data | Partial |
| Missing Item Recovery | Partial |
| Item Icons and Visuals | Partial |
| Loot and Reward Integration | Partial |

The biggest remaining job is standardizing custom item definitions and ensuring they interact correctly with the game's existing crafting, inventory, quality, and enhancement systems.

---

## 7. Research and Progression

**Priority: HIGH**

Foundation already has substantial custom research integration.

| Feature | State |
|---|---|
| Technology Registration | Implemented |
| Technology Tree Integration | Implemented |
| Custom Requirements | Partial |
| Research Dependencies | Partial |
| Research Rewards | Partial |
| Custom Unlocks | Partial |
| Prestige Integration | Partial |
| Ascension Integration | Partial |
| Nightmare Progression | Partial |
| Technology Removal Recovery | Partial |
| Research UI Extensions | Partial |
| Research Tree Layout Management | Partial |

The goal is to make it possible for mods to add research and progression content without having to patch the technology tree themselves.

---

## 8. Adventure and Combat Framework

**Priority: HIGH**

The source includes systems for quests, dungeons, wars, characters, training, and combat. Foundation provides some access already, but additional native operations need dedicated interfaces.

| Feature | State |
|---|---|
| Quest Registration | Implemented in source |
| Quest Progression | Partial |
| Dungeon Registration | Implemented in source |
| Dungeon Progression | Partial |
| War Registration | Implemented in source |
| War Progression | Partial |
| Combatant Access | Implemented |
| Equipment Management | Partial |
| Combat Statistics | Partial |
| Damage and Healing | Implemented |
| Training | Partial |
| Character Recruitment | Partial |
| Rewards | Partial |
| Character Progression | Partial |
| Adventure UI Integration | Partial |

This should ultimately support mods that add custom adventure content without having to recreate the native adventure systems.

---

## 9. World and Workspace Editing

**Priority: HIGH**

| Feature | State |
|---|---|
| Workspace Discovery | Implemented |
| Workspace Creation | Implemented |
| Workspace Switching | Implemented |
| Workspace Renaming | Implemented |
| Machine Placement | Implemented |
| Machine Removal | Implemented |
| Machine Movement | Implemented |
| Pipe Editing | Partial |
| Decoration Placement | Implemented |
| Decoration Removal | Implemented |
| Workspace Cloning | Missing |
| Multi-Machine Selection | Missing |
| Batch Placement | Missing |
| Batch Removal | Missing |
| Machine Copy/Paste | Missing |
| Workspace Templates | Missing |
| Blueprint Integration | Missing |
| Undo/Redo Integration | Missing |

For the missing features, Foundation should first reuse any existing native behavior. Additional abstractions should only be introduced where the game doesn't provide an appropriate operation.

---

## 10. Asset and Content Creation

**Priority: HIGH**

Foundation should provide a standardized way for mod developers to register content.

| Feature | State |
|---|---|
| Sprite Loading | Implemented |
| AssetBundle Loading | Implemented |
| Native Icon Registration | Implemented in source |
| Localization | Partial |
| Custom Machine Assets | Partial |
| Prefab Registration | Partial |
| Asset Ownership | Partial |
| Asset Cleanup | Partial |
| Custom UI Assets | Partial |
| Audio Asset Registration | Missing dedicated API |
| Content Definition Validation | Partial |
| Content ID Management | Partial |
| Asset Dependency Management | Missing |
| Resource Caching | Partial |

**Major remaining task:** Establish a consistent content registration process that handles definitions, assets, persistence, and native integration together.

---

## 11. Events and Hooks

**Priority: HIGH**

Foundation already has a typed event system. It needs additional coverage across native gameplay.

| Feature | State |
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
| Event Priority | Missing |
| Cancellable Events | Missing |
| Event Filtering | Partial |

A centralized event system would allow mods to share event subscriptions instead of independently patching identical game methods.

---

## 12. Mod Interoperability

**Priority: CRITICAL**

This is especially important if Foundation becomes the primary framework used by other Kingdom Inc. mods.

| Feature | State |
|---|---|
| Mod Discovery | Partial |
| Mod Metadata | Partial |
| Dependency Registration | Partial |
| Cross-Mod Service Access | Missing |
| Shared Interfaces | Partial |
| Mod Compatibility Detection | Partial |
| Shared Content References | Missing standardized API |
| Mod Conflict Detection | Missing |
| API Version Negotiation | Missing |
| Dependency Load Ordering | Partial |
| Cross-Mod Events | Implemented |
| Optional Mod Integrations | Missing standardized API |

### Required Outcome

Two mods should be able to interact through Foundation without requiring direct references to each other's implementation DLLs.

For example, one mod could expose a service, and another could discover and use that service through Foundation.

---

## 13. Developer SDK and Tooling

**Priority: MEDIUM**

| Feature | State |
|---|---|
| Example Research Mod | Implemented |
| Example Services Mod | Implemented |
| Example Gameplay Mod | Implemented in source |
| Developer Console | Implemented |
| Diagnostics | Implemented |
| Native API Bridge | Implemented in source |
| API Documentation | Partial |
| Example Machine Mod | Missing |
| Example UI Mod | Partial |
| Custom Content Templates | Missing |
| Mod Project Generator | Missing |
| API Reference Documentation | Partial |
| Developer Debug Panels | Partial |
| Compatibility Guidelines | Partial |
| Migration Guides | Missing |

The biggest improvements here are documentation, complete examples, and a simple way to bootstrap new mods.

---

## Overall Priority

Based on the goal of exposing everything the game can do, the remaining work should be completed in this order:

| Priority | Workstream | Goal |
|---|---|---|
| **P0** | Core Framework | Complete mod registration, lifecycle, and service architecture |
| **P0** | Machine Framework | Provide dedicated functionality for every native machine |
| **P0** | UI Framework | Complete reusable native controls and machine-specific panels |
| **P0** | Signal System | Complete source/property configuration and persistence |
| **P0** | Mod Interoperability | Allow independent mods to communicate through Foundation |
| **P1** | Native IO | Complete connection editing, port configuration, and throughput access |
| **P1** | Content Registration | Standardize custom machines, items, recipes, upgrades, and assets |
| **P1** | World Editing | Complete workspace and bulk-editing operations |
| **P1** | Research | Complete custom research and progression integration |
| **P1** | Adventure & Combat | Complete native gameplay access |
| **P1** | Economy & Production | Complete resource and production operations |
| **P1** | Persistence | Complete content compatibility and schema migration handling |
| **P2** | Events | Expand native event coverage |
| **P2** | Developer Tools | Finish debugging and development utilities |
| **P2** | SDK & Documentation | Complete APIs, examples, and documentation |
| **P3** | Foundation 1.0 | Consolidate the stable public API and produce the release |

---

## Essential Requirements Before Foundation 1.0

Three architectural gaps are particularly important.

### 1. Complete Native Game Coverage

Every supported machine and gameplay system should have discoverable functionality, even if certain specialized operations are exposed through the native bridge rather than dedicated convenience methods.

### 2. Centralized UI Generation

Mods should be able to register their own machines, signals, properties, and configuration controls without constructing every panel from scratch.

### 3. Cross-Mod Interoperability

Foundation should provide shared service discovery, lifecycle management, common interfaces, and consistent content references.

These are more important right now than adding another standalone diagnostic system or another example mod.

## Current Assessment

**Foundation 0.10.1 has broad gameplay API coverage, but it is not a complete modding platform yet.** The key missing work is turning those individual integrations into standardized systems that any mod can use consistently.

Development should continue with the **Core Framework, Machine Framework, UI Framework, Signal System, and Mod Interoperability together**, because those systems depend heavily on one another.

The goal is **a single framework through which mods can access, extend, and configure Kingdom Inc.'s native functionality**.
