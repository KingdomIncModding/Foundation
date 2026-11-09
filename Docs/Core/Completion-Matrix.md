> **Stage B1 update (0.10.7):** The six B1 roadmap candidates were rewritten/consolidated as documented in `Stage-B1-Rewrite.md`. The historical Core 0.10.1 table below is retained for source lineage; see the new report for native-boundaries, single-bus events, unified module directory, ownership recovery, typed service discovery and new runtime requirements.

# Foundation Core — Implementation Matrix

**Core API:** `KingdomFoundation.Core` 1.0.0 (additive source extension for the user's Foundation 0.10.1). **Runtime integration:** second BepInEx class in the existing Foundation.dll, not a separate assembly or mod loader.

| Core requirement | Implemented in | Behavior |
|---|---|---|
| BepInEx integration | `Runtime/FoundationCorePlugin.cs` | Loads through the game's already-installed BepInEx 5, requires the existing Foundation plugin. |
| Centralized mod registration | `Modules/CoreModuleRegistry.cs`, `ModRegistration.cs` | Unique module IDs, metadata, activation state and cleanup handle. |
| Mod dependencies and ordering | `Modules/ModuleDependencyResolver.cs`, `Runtime/CorePluginCatalog.cs` | Hard/optional module and plugin dependencies, min versions, deterministic topological activation, cycle detection. |
| Cross-mod services | `Services/CoreServiceRegistry.cs`, `Modules/OwnedServices.cs` | Shared stable service IDs, contracts, version checks, safe ownership/lifetime, register/unregister notifications. |
| Mod lifecycle | `Modules/CoreModuleRegistry.cs`, `Models/WorldChangedEvent.cs` | Initialize, optional world enter/exit, Shutdown, state change events, save load/store observations. |
| Harmony integration | `Patches/CorePatchSession.cs`, `CorePatchManager.cs` | Distinct Harmony owner per module, patch-class registration, owned unpatch, target overlap inspection. |
| Unity main-thread dispatcher | `Runtime/CoreDispatcher.cs`, `Modules/OwnedDispatcher.cs` | Worker-thread callbacks queued for the next Unity update; per-mod cancellation and isolated errors. |
| Framework services | `Contracts/IFoundationCore.cs`, `Modules/ModContext.cs` | One entry point, module-scoped service/event/patch/content wrappers. |
| Error isolation | `Diagnostics/CoreDiagnostics.cs`, `Events/CoreEventBus.cs`, `Modules/CoreModuleRegistry.cs` | Subscriber, initializer, world callback, shutdown, and disposal failures isolated and recorded. |
| API version compatibility | `Runtime/CoreApiVersion.cs`, `Models/ModDescriptor.cs` | Stable major API contract and minimum-version requirements for modules/services. |
| Mod unloading | `Modules/ModScope.cs`, `Modules/CoreModuleRegistry.cs`, `Services/CoreServiceRegistry.cs` | Managed callbacks/patches/services cleaned; native permanent content IDs not removed from live game. |
| Mod metadata | `Models/ModDescriptor.cs`, `Runtime/CorePluginCatalog.cs` | ID, name, author, description, version, requirements, loaded BepInEx plugin versions. |
| Central content registry | `Services/CoreContentCatalog.cs`, `Modules/OwnedContent.cs` | Typed kind/name ownership, duplicate IDs, version, lifetime and availability; native game registrations remain managed by Foundation's existing content APIs. |
| Source organization | `src/Foundation/` | One top-level type per file, nested Harmony/helper types in separate partial fragments, domain-specific folders and original names/patch identities preserved. |

## Core source layout

`Foundation.csproj` includes the reorganized `src/Foundation/**/*.cs`, including the 75 Core type files under `src/Foundation/Core/{Contracts,Models,Runtime,Modules,Services,Events,Patches,Lifecycle,Diagnostics}`. The independent example is `Examples/CoreMod` and the owner-facing command is `INSTALL-FOUNDATION.cmd`. No one-off organizer is shipped.

## API boundaries

`FoundationCoreApi.Core` provides lifecycle, cross-mod services, patch ownership and shared metadata. `FoundationApi` continues to provide the real game APIs and remains authoritative for native game content.

The Core registry does not manufacture native items by ID: register real items, recipes, machines and quests through the native gameplay services, then register their ownership metadata in Core. Native static game registries must remain present while save data referencing them is loaded.

## Release status

The combined 0.10.1 + Core source was reorganized against the actual source ZIP supplied in this conversation and checked for layout and brace integrity. The combined code has not been compiled or game-tested in this environment. The older 0.9.3 audit does not establish 0.10.1+Core runtime compatibility.
