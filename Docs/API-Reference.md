# Foundation API Reference

The primary API is `KingdomFoundation.Api.FoundationApi`.

| Property or entry point | Purpose |
| --- | --- |
| `World`, `Workspace` | Current kingdom and workspace editing |
| `Machines`, `IO`, `NativeIO` | Game machines and native logistics |
| `Items`, `Production`, `Economy` | Items, crafting and currencies |
| `Progression`, `Adventures`, `Combat` | Research, quests, war and combat |
| `View`, `Audio`, `Presentation` | Camera/input, audio and native assets |
| `Content`, `TechNodes`, `WorkspaceNodes` | Supported mod-created definitions |
| `Panels`, `ModBar`, `ModConfig`, `NativeUiFactory` | Native-themed mod interfaces |
| `Signals`, `SignalDirectory`, `SignalLinks` | Signal source/property discovery and bindings |
| `Assets`, `AssetDirectory` | Mod-owned assets |
| `ModSave`, `SaveMigrations` | Per-kingdom mod-owned persistence |
| `Events`, `Lifecycle`, `Diagnostics` | Events, lifecycle and diagnostics |
| `ForMod(modId)` | Owner-scoped registration context |

Additional entry points:

- `KingdomFoundation.Machines.FoundationMachineApi` provides machine-specific discovery, inspection, supported commands and native IO.
- `KingdomFoundation.Core.FoundationCoreApi.Core` provides module registration, dependency management, shared services and events.

For exact signatures and argument types, see `src/Foundation/Api/` and `src/Foundation/Core/Contracts/`. This index groups interfaces by capability rather than their implementation history.
