# Foundation 0.10.6 — Native Machine Game API (A1) SDK

**Scope:** real Kingdom Inc `FlowElement` and its native subclasses. This is a Stage A game-backed API, **not** the Stage B mod-created machine/content registration SDK or UI rewrite.

**Lifetime/threading:** Call from Unity's main thread, while a kingdom is loaded. Do not hold references to removed elements or across kingdom unload. `Registry.All` returns live native elements, while `Inspect` returns a captured view. Mutators require a placed machine with the correct native `WorldFlow*` component.

## Discover native machine types

```csharp
using KingdomFoundation.Machines;
using Assets.Source.Flow.Elements;

// No live kingdom required to enumerate types documented in the supplied
// native source. These are *types*, not unlocked/placed machines.
var classes = FoundationMachineApi.NativeTypes.KnownTypes;

// Enter a kingdom first. These are the types the native prefab catalog actually
// registers; do not assume every class in source is a player-buildable item.
var nativePrefabs = FoundationMachineApi.NativeTypes.RegisteredTypes();

// Native placed machines, including any mod-created subclasses:
var machines = FoundationMachineApi.Registry.All();

// Each concrete native class is individually discoverable by its CLR type:
var collectors = FoundationMachineApi.Registry.OfType<Collector>();
```

`Describe(machine)` returns `null` when the exact runtime class is not one of the 61 game-origin classes in the pinned source. This **does not** prevent generic FlowElement or mod extension access; it prevents accidentally describing a mod-added subclass as vanilla.

## Snapshot native state separately from custom extensions

```csharp
foreach (var machine in FoundationMachineApi.Registry.All())
{
    var snapshot = FoundationMachineApi.Registry.Inspect(machine);
    var className = snapshot.NativeType?.NativeType.Name ?? "Mod/other";
    // Real machine-specific scalar values and native flags:
    var nativeFacts = snapshot.NativeProperties;
    // Registered mod-authored properties, unchanged from earlier Foundation:
    var modProperties = snapshot.AdditionalProperties;
    // Base native fields remain available: Identity, Inputs/Outputs,
    // UpgradeSlots, Progress, Productivity, CanManualCraft, etc.
}
```

`NativeProperties` uses consistent string keys for diagnostics and read-only consumers, not a second persisted machine record. The underlying native `FlowElement` still owns all authoritative state. It never exposes mutable `InventoryItem`, `List<>`, or party references through the snapshot map. The current key set is recorded in `Docs/GameAPI/A1-Native-Machine-Coverage.md` and implemented by `MachineSpecializations.Read`.

## Configure only native-supported operations

```csharp
var ops = FoundationMachineApi.Operations;

// Only use an actual placed game machine; `SetAccountant`, `SetAppraiser`,
// and `SetWarType` update the native machine and its WorldFlow* view.
// Argument values must be real native enums / WarType instances.
// Check result.Succeeded and result.Message before treating it as applied.

// Native logistics behavior -- no Foundation routing simulation:
foreach (var collector in FoundationMachineApi.Registry.OfType<Collector>())
{
    var result = ops.AdvanceCollector(collector);
}

// No new UI framework is needed to inspect available machine actions.
foreach (var machine in FoundationMachineApi.Registry.All())
{
    var supported = FoundationMachineApi.Capabilities.SupportedCommands(machine);
}
```

The existing `FoundationApi.Machines` public service and `MachineOperations` remain accessible and backward-compatible. They already expose native recipe selection, resource selection, filtering, timer/switch, portal, armory, quest and dungeon actions, manual crafting, IO expansion/collapse, and pipe editing (A2). This release adds safe specialized accessors without duplicating those bridges.

### Purchase costs, recipe costs and resources

```csharp
var costs = FoundationMachineApi.Production;
foreach (var machine in FoundationMachineApi.Registry.All())
{
    var nativePurchaseCost = costs.NativePurchaseCost(machine);
    // Returns the game-calculated currency collection for *this* player.
    // Never assume these values are universal or immutable.
}
```

For `RecipeElement` use `AvailableRecipes`, `SelectedCraftingTime`, and `NativeCraftingCost`. For `ResourceElement` use `AvailableResources`, `NativeOutputRate` and `NativeResourceCost`; `Forest` has its own native `NativeForestCost`. None estimate item-pipe throughput, which is covered by verified A2 and must not be conflated with the game's production and resource rates.

## What is deliberately not provided

- No invented machine types, processing rules, economy calculations, resource routing, custom transport or made-up throughput limits.
- No new panel registry or machine-specific mod editor (Stage B4).
- No writable pseudo-settings for fields that game code does not support configuring safely. The real Outfitter.Timeout field is writable only with its native research requirement; its UI slider remains game-owned.
- No synthetic fixed lists of “unlocked” machines; the real game prefab and technology state are consulted.
- No second authoritative game state cache or save format.

**Source and runtime status:** source-verified against the supplied original game classes, but Windows compilation, per-type in-game inspection, save/reload and regression testing remain separate required acceptance steps.
