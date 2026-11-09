# Foundation 0.10.0 — Kingdom Inc Native Gameplay SDK

## Purpose

Foundation is the single BepInEx 5 plugin responsible for the Kingdom Inc integration layer. Every gameplay service below calls the actual game implementation found in `Assembly-CSharp.dll`. The framework deliberately does **not** create a parallel simulation, synthetic IO throughput, or independently interpreted world state.

The original Foundation features remain: tech-node integration, workspace-node registration, the native Mod Bar, mod configuration and installed-mods UI, Nexus notices, pipe inspection/connection operations, save extension, signal registry, developer console, missing-content recovery, and typed event/lifecycle services.

**Version:** 0.10.0 (expanded *source*; 0.9.3 was the last build confirmed by a real Windows game audit). The source archive is ready to build but is not itself a compiled distribution. Native game/BepInEx DLLs are intentionally not redistributed.

## Public gameplay services

All are available through the static `KingdomFoundation.Api.FoundationApi` class.

| Property | Main native APIs exposed |
| --- | --- |
| `View` | native mouse / game input, camera zoom/pan and selection-related state |
| `Audio` | native music playback, cue queuing and volume |
| `World` | `GamePlayer.Current`, worlds/workflows, element querying, workflow purchase/selection/name/camera |
| `Workspace` | `WorldFlowManager`, native purchase/place/remove/move, decorations, manual craft and upgrade slots |
| `Machines` | catalog and type-filter search; native recipe, filter, switch, timer, appraiser, routing, armory/portal, quest/dungeon and resource machine actions |
| `IO` | validation, connections/disconnections and measured transfer rates from native flow pipes |
| `Flow` | live read-only element/IO/pipe snapshots and connection/transfer events |
| `Economy` | currencies, costs, resource quota/demand/production, grants, spending and farm quotas |
| `Progression` | research, unlocked machine upgrades, prestige, ascension and nightmare settings |
| `Adventures` | quest/dungeon/war catalogs, progress, unlocked state and shared expedition/party stores |
| `Combat` | native combatant/equipment stats, damage, healing, energy and training |
| `Items` | item and crafting catalogs, item generation/clone/serialization, buffer input/output |
| `Content` | permanent game-native registration for items, crafting recipes, quests, dungeons, wars, prestige and enhancements |
| `Upgrades` | custom `FlowUpgrade` factories routed through the native JSON creation path |
| `Production` | forestry workforce/quotas; farm planting, unlocking and expansion |
| `Logistics` | native armory/portal storage channels, counts, available types and enqueue |
| `UI` | open/close the game's actual build, item, research, resource, war, farm, forest and dungeon menus; dialogs and native actions |
| `Presentation` | register native SpriteLibrary icons and native Translation text, locale, icon catalog |
| `PropertyControls` | automatically produce Mod Settings editors for writable boolean, enum, numeric and color signal properties |
| `NativeSignals` | expose a selected set of real public native fields/properties through the discoverable signal/property registry |
| `Native` | enumerate any native game type and its public members, construct, invoke, read and write them as an extension escape hatch |
| `Flags` | namespaced native `Register` flags, counters and strings, saved by the game |
| `Saves` | list/read native saves; snapshot, save and autosave through `SaveGame` |
| `Runtime` | owner-tagged per-frame callbacks and keyboard shortcuts |
| `Events` | typed, owner-scoped subscriptions with exception isolation |
| `Lifecycle` | base-game world/load/save lifecycle events |
| `Signals` | register typed source properties and listen for changes |
| `ModSave` | versioned, per-mod sections in real kingdom saves |
| `Assets` | load PNG sprites and Unity AssetBundles with ownership |
| `TechNodes` | register research directly in vanilla tech tree (existing system) |
| `WorkspaceNodes` | native flow element registration and persistence (existing system) |
| `ModBar` | native-styled custom HUD (existing system) |
| `ModConfig` | native-styled options and custom controls (existing system) |
| `Diagnostics` | scoped errors and recent diagnostic history |

The `Native` bridge is deliberately restricted to **public** game methods/fields/properties on types in the actual `GamePlayer` assembly. It is not a fictional API generator; callers supply real reflected native types/methods and remain responsible for semantic correctness.

## Core example — inspect the active kingdom

```csharp
using KingdomFoundation.Api;
using Assets.Source.Flow.Elements;

if (FoundationApi.World.IsInKingdom)
{
    var current = FoundationApi.World.ActiveFlow;
    foreach (var element in FoundationApi.World.FindElements(e => e is TrackSwitch))
        UnityEngine.Debug.Log(element.Guid + " " + element.Position);

    var switchNode = FoundationApi.Machines.Find<TrackSwitch>();
    UnityEngine.Debug.Log("Switches: " + switchNode.Count);
}
```

## Native machine operations

These are real game methods, not replacement state. For example, recipe changes delegate to the game's `WorldFlowRecipe.SetRecipe` so the game handles disconnect/reconnect; Forestry delegates to `WorldFlowForest.SetResource`. Avoid writing `RecipeElement.Recipe` directly or appending ports manually.

```csharp
var input = FoundationApi.Machines.Find<Filter>();
if (input.Count != 0)
{
    var item = FoundationApi.Items.FindItem("iron_ore"); // supply an actual registered ID
    if (item != null) FoundationApi.Machines.SetFilter(input[0], item);
}
```

The `Machines` API also supports `TrackSwitch`, `Timer`, `Appraiser`, `Distributor`, `Manifold`, `Collector`, `Armory`, `Portal`, `QuestBoard`, `Dungeon`, `RequisitionOffice`, `Shop`, `RecipeElement` and `Forest`. All require an element placed in the current kingdom.

## Native signal-source/property selection and automatic options editors

```csharp
private FoundationModContext _mod;
private bool _enabled = true;
private UnityEngine.Color _color = UnityEngine.Color.green;

void Awake()
{
    _mod = FoundationApi.ForMod("author.modid");
    var source = _mod.Expose(new SignalSourceDefinition("author.modid", "author.modid:controller", "Controller", new[]
    {
        new SignalPropertyDefinition("enabled", "Enabled", typeof(bool), () => _enabled, v => _enabled = (bool)v),
        new SignalPropertyDefinition("color", "Color", typeof(UnityEngine.Color), () => _color, v => _color = (UnityEngine.Color)v),
    }));

    var settings = _mod.Settings("Controller", "Controller options");
    FoundationApi.PropertyControls.Build(settings, source);
    settings.AddSignalSelector("Signal Source + Property", new SignalSelection(null, null), selection =>
        UnityEngine.Debug.Log(selection.ToString()));
}
void OnDestroy() => _mod?.Dispose();
```

Foundation creates native-themed controls from the available properties. Colors are rendered as direct-click rectangles (no color dropdown or wheel) and participate in Mod Settings Apply/Cancel.

To explicitly expose native game properties rather than mod-owned values, use `_mod.ExposeNative(id, label, typeof(GamePlayer), () => GamePlayer.Current, new[] { "GameTime", "CurrentFlow" })`. Only selected public members are exposed; this is not unrestricted recursive serialization. Only register this when its target is available during source polling (or write a null-safe source getter).

## Events

`FoundationApi.Events.Subscribe<T>(modId, callback)` works for `WorkspaceChangedEvent`, `KingdomFlowCreatedEvent`, `CurrencyChangedEvent`, `ResourceChangedEvent`, `ResearchUnlockedEvent`, `QuestCompletedEvent`, `DungeonStageChangedEvent`, `WarStageChangedEvent`, `DungeonUnlockedEvent`, `WarUnlockedEvent`, `PrestigeChangedEvent`, `KingdomAscendedEvent`, `MachineRecipeChangedEvent`, `MachineFilterChangedEvent`, `MachineUpgradeChangedEvent`, `MachineQuestChangedEvent`, `MachineDungeonChangedEvent`, `ItemQualityChangedEvent`, and the existing pipe/flow and lifecycle events. `FoundationModContext.Listen<T>` tracks/unsubscribes automatically on disposal.

```csharp
_mod.Listen<MachineRecipeChangedEvent>(e =>
    Logger.LogInfo($"{e.Machine.ElementType}: {e.Previous?.Identifier} -> {e.Current?.Identifier}"));
_mod.Listen<CurrencyChangedEvent>(e => Logger.LogInfo(e.Currency + " delta=" + e.Delta));
```

Events are observational: they are fired after the base game's operation; Foundation subscribers cannot cancel native operations. Native workflow item pipes remain independent of mod signal properties.

## Mod content registration and save stability

Register persistent content in **plugin Awake, before any `GamePlayer` is created**. Item, recipe, quest, dungeon, war and prestige IDs are prefixed `modGUID:local-id`. Native item IDs with `:` require custom load handling; Foundation's `ItemIntegration` patch recognizes registered IDs as the complete item prefix then parses `:q<quality*1000>` and `:s<seed>` suffixes, while leaving unregistered vanilla strings on the game's original parser.

**Special case: native enhancements.** The game's `ItemEnhancementData.FromJson` splits at the *first colon* to parse quality. To stay compatible, enhancement IDs must instead be `modGUID.local-id` (dot separator; no colon). `FoundationApi.Content.RegisterEnhancement` enforces this. This is a native serialization constraint, not an arbitrary framework choice.

Custom `FlowUpgrade` implementations must override `UpgradeType` to return their registered namespaced ID, and factory registration must happen before a save containing that upgrade loads.

`Content` and `Upgrades` are permanent registrations during the game process. Don't dispose them mid-world: base-game saves use identifiers and collection indices and cannot safely have native types removed. Existing mod-owned data (registered by `ModSave`) remains separate and is retained if a mod becomes unavailable.

## Native localization and icons

```csharp
FoundationApi.Presentation.RegisterText("author.modid", "machine_name", "en-US", "My Machine");
// Native Translation.Translate("@author.modid:machine_name")

var loaded = FoundationApi.Assets.LoadSprite("author.modid", "icon", pngFilePath);
FoundationApi.Presentation.RegisterSprite("author.modid", "icon", loaded.Sprite);
// Set machine.IconName = "author.modid:icon" so the game's SpriteLibrary can resolve it.
```

Use actual supported Unity asset load methods. Sprites referenced by persistent content should remain loaded for the duration of the kingdom session. The stock locale's translation keys fall back via the game's own `Translation` implementation.

## Optional game reflection bridge

```csharp
var native = FoundationApi.Native;
var type = native.FindType("Assets.Source.Player.GamePlayer");
var current = FoundationApi.World.Player;
object gameTime = native.Read(current, type, "GameTime");
var available = native.Members(type); // public fields/properties/methods/events
```

The bridge is an intentional advanced escape hatch. It cannot guarantee a game's private mechanics or a newer game build will behave the same way. Always use the typed service when available.

## Setup and compilation

The standard installer uses the known Kingdom Inc game root supplied by the user (`G:\Program Files (x86)\Steam\steamapps\common\KingdomInc`), builds Foundation with installed BepInEx 5 and game-managed assemblies, packages, and installs it. Runtime verification is **optional** (`-Verify`), not a repeated required user step. To install simply use `INSTALL-FOUNDATION.cmd` from the source directory. To build without install: `INSTALL-FOUNDATION.cmd -BuildOnly`.

Foundation's runtime `Foundation.dll` is the only player plugin. Examples compile into independent DLLs for mod developers and are never copied to the player's `BepInEx/plugins/Foundation` folder. The framework should not redistribute licensed game DLLs or the user's save files.

## Boundaries

- Only the existing game's public APIs and confirmed Harmony interception points are used for core behavior. Foundation does not invent a second economy, crafting, combat or pipe engine.
- Native game content APIs are often **not hot-unloadable**. Registration is permanent until game exit; some native registries have fixed-size arrays.
- Direct public reflection can still corrupt game state if used incorrectly; use typed services where possible.
- Save schema preservation depends on the native serialization format and mod presence. Backups are still important for modded worlds.
- The expanded 0.10.0 code has been inspected against supplied source but not compiled or launched here. Successful 0.9.3 audit does not retroactively prove 0.10.0 works at runtime.

**Public SDK samples:** `Examples/ExampleTechNodes`, `Examples/ExampleServices`, and `Examples/ExampleGameplay`.


> **Current organization:** `src/Foundation/` is the only production C# source root. The installer is `INSTALL-FOUNDATION.cmd` and reusable scripts are in `Tools/`. `Docs/Core/` contains the Core Framework documentation.
