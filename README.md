# Foundation

### A unified modding framework for Kingdom Inc.

**Foundation** gives mod developers a shared C# API for extending *Kingdom Inc* without rebuilding the game's systems from scratch. It brings native gameplay access, custom content registration, in-game interfaces, mod-to-mod services, and persistence together in one framework.

Instead of every mod maintaining its own UI, event hooks, machine discovery, and save integration, Foundation provides reusable interfaces that mods can build on.

> **Foundation 1.0 Alpha** · C# · Unity · BepInEx 5  
> One runtime assembly: `Foundation.dll`

## What can you do with Foundation?

### Create content that belongs in the game

Register mod-owned content through the game's existing systems, including:

- **Research and technology** — add technologies and upgrade branches to the native research tree, with costs, prerequisites, and unlock callbacks.
- **Workspace nodes** — introduce custom node types with their own factories, settings, and native workspace integration.
- **Items and production content** — register items, crafting recipes, upgrades, and other supported native definitions.
- **Adventures and progression** — work with quests, dungeons, wars, prestige, and other game-backed progression data.

Foundation keeps the game's original models and identifiers at the center of these integrations. It is not a second gameplay simulation.

### Work with machines and logistics

Inspect native machines and their behavior through structured APIs instead of scattering game-specific reflection throughout a mod.

- Discover machine types and instances in the current kingdom.
- Read machine properties, recipes, capabilities, and production information.
- Inspect input/output ports, connections, buffers, and observed throughput.
- Interact with supported native machine operations while respecting game rules.
- Respond to machine, workspace, and item-transfer events.

**Native item flow and Foundation signals are different systems.** Native pipes continue to use the game's own logistics rules; Foundation's signal system is for mod-defined values and bindings.

### Make interfaces that feel native

Foundation's UI APIs are designed to sit alongside *Kingdom Inc*, not on top of it as an unrelated-looking overlay.

- Mod Bar buttons, toggles, dropdowns, labels, and custom elements.
- Custom panels and machine-specific configuration interfaces.
- Mod settings integrated with the game's options UI.
- Reusable controls for input, selection, tabs, collapsible sections, dialogs, and property editing.
- Game-derived styling and assets where available.

Mods can share a consistent visual language while still creating interfaces suited to their own features.

### Connect mods to each other

Foundation also provides the infrastructure behind more complex mod ecosystems:

| Capability | What it provides |
| --- | --- |
| **Mod lifecycle** | Registration, initialization, dependencies, and cleanup. |
| **Shared services** | Discover and consume services published by other mods. |
| **Event system** | Owner-scoped subscriptions to Foundation and game-backed events. |
| **Signals** | Expose typed properties and connect compatible sources and destinations. |
| **Persistence** | Save mod-owned data with versioned schemas and migration support. |
| **Assets** | Load and manage mod-owned sprites and asset bundles. |
| **Game services** | Access supported world, machine, item, economy, progression, combat, UI, camera, and audio information. |

## Your first Foundation mod

A Foundation mod is a regular BepInEx plugin that depends on the Foundation runtime. The simplest approach is to create an **owner-scoped mod context** and let it manage your registrations.

```csharp
using BepInEx;
using KingdomFoundation.Api;

[BepInPlugin(ModId, "My Foundation Mod", "1.0.0")]
[BepInDependency("urakka.kingdominc.foundation")]
public sealed class MyFoundationMod : BaseUnityPlugin
{
    private const string ModId = "example.myfoundationmod";
    private FoundationModContext context;

    private void Awake()
    {
        context = FoundationApi.ForMod(ModId);

        var settings = context.Settings(
            "My Foundation Mod",
            "Settings provided through Foundation.");

        settings.AddToggle("Enable feature", true, enabled =>
            Logger.LogInfo("Feature enabled: " + enabled));

        context.Listen<WorkspaceChangedEvent>(change =>
            Logger.LogInfo("Workspace changed: " + change.Kind));
    }

    private void OnDestroy() => context?.Dispose();
}
```

This example adds a mod settings entry and listens for workspace changes. The context tracks subscriptions and other disposable registrations so they can be released when the plugin unloads.

For more complete examples, browse [`Examples/`](Examples/) — including Core modules, game services, research, gameplay events, and UI integrations.

## The API at a glance

Foundation exposes three complementary entry points:

| Entry point | Use it for |
| --- | --- |
| `FoundationApi` | High-level access to gameplay services, UI, content, signals, assets, and persistence. |
| `FoundationApi.ForMod(modId)` | Owner-scoped registrations, event subscriptions, settings, and cleanup. |
| `FoundationCoreApi.Core` | Core module registration, lifecycle, shared services, dependencies, and inter-mod infrastructure. |

Machine APIs also live in the **same** `Foundation.dll` under the `KingdomFoundation.Machines` namespace. A separate machine-framework runtime is not required.

### Architecture

```text
Your BepInEx Mod
       │
       ▼
  Foundation.dll
       │
       ├── Core: modules, services, lifecycle, events
       ├── Game API: native world, machines, IO, progression
       ├── Content: research, items, recipes, workspace nodes
       ├── UI: Mod Bar, panels, settings, native-themed controls
       └── Extensions: signals, assets, mod-owned persistence
       │
       ▼
Kingdom Inc's existing game systems
```

**Design principle:** Foundation should expose and extend the game's behavior wherever possible, not replace its authoritative mechanics.

## Documentation

Developer documentation is organized by capability:

| Guide | Description |
| --- | --- |
| [Documentation index](Docs/README.md) | Start here |
| [Getting started](Docs/Getting-Started.md) | Create a Foundation mod |
| [API reference](Docs/API-Reference.md) | Public service map |
| [Core framework](Docs/Core/README.md) | Modules, events and inter-mod services |
| [Game API](Docs/GameAPI/README.md) | Native Kingdom Inc systems |
| [Machines](Docs/GameAPI/Machines.md) | Machine capabilities and operations |
| [Logistics](Docs/GameAPI/Logistics.md) | Native pipes, ports and throughput |
| [Gameplay](Docs/GameAPI/Gameplay.md) | Items, research, adventures and world state |
| [UI](Docs/UI/README.md) | Panels, HUD controls and settings |
| [Signals](Docs/Signals.md) | Typed source/property bindings |
| [Content and persistence](Docs/Content-And-Persistence.md) | Mod content and saved data |
| [Migration](Docs/Migration.md) | Updating older mods |

### Repository layout

```text
src/Foundation/   Framework and game API source
SDK/              Public SDK documentation
Examples/         Example Foundation-powered mods
Docs/             Architecture, feature guides, and API references
```

## Compatibility and design notes

- Foundation is intended for the **PC version of Kingdom Inc** running through **BepInEx 5**.
- Register content using **stable mod and content IDs**. The game may store references to those IDs in saved kingdoms.
- Some native content registrations have **process-lifetime constraints**; removing a registration while its content is in use is not the same as removing an ordinary event subscription.
- Mods previously referencing the separate `Foundation.Machines.dll` need to reference the unified `Foundation.dll` instead.
- Foundation 1.0 is currently labeled **Alpha**. Public APIs may evolve; consult the migration guide when updating an existing mod.

---

**Foundation is a community modding project for Kingdom Inc.** It is not an official game component or a replacement for the game itself.
