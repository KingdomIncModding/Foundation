> **Current 0.10.7 B1 implementation:** See `Stage-B1-Rewrite.md` for current discovery, lifecycle and inter-mod contract behavior. The 0.10.1 source-only notes below are a baseline, not the current runtime signoff.

# Foundation Core API 1.0

**Target:** existing Kingdom Inc Foundation 0.10.1 source tree. The Core overlay is additive and does **not** replace working game, machine, signals, settings, or save integrations.

## What Core provides

| Core capability | Implementation |
|---|---|
| Bootstrap | `FoundationCorePlugin` is an additional BepInEx plugin class inside the existing Foundation.dll, dependent on `urakka.kingdominc.foundation`. No changes to `FoundationPlugin.Awake()` are required. |
| Mod registration and metadata | `IFoundationModule`, immutable `ModDescriptor`, `IModRegistration`, `CoreModuleRegistry` with unique ID enforcement. |
| Dependency validation | Foundation-module and installed BepInEx-plugin dependencies; minimum versions, optional dependencies, cycle detection, deterministic topological activation. |
| Shared service registry | `ICoreServices` supports typed and reflection-based lookup, version requirements and ownership. `IModServices` restricts registrations to the current module ID. |
| Cross-mod events | Typed `Subscribe<T>` / `Publish<T>`, filters, deterministic priorities, once handlers and isolated exceptions. |
| Ownership and cleanup | `ModScope`, managed callback/subscription/service disposal, world-lifetime disposal; native permanent content is never hot-unregistered. |
| Harmony | Distinct patch-owner Harmony IDs; owner-scoped unpatching and conflict discovery. Foundation doesn't unpatch other mods' Harmony methods. |
| Game lifecycle | Live GamePlayer transition observation, module `WorldLoaded/WorldUnloading`, native SaveGame load/save observation patches. |
| Central content IDs | `ICoreContentCatalog` for namespaced ID/owner/descriptor metadata, duplicate prevention, world and permanent lifetimes. This **does not itself create native game content**; call the existing `FoundationApi.Content` to register actual items/recipes/nodes. |
| Diagnostics | Bounded diagnostic history, BepInEx logging, per-module failure isolation and lifecycle state snapshots. |
| API compatibility | Stable `KingdomFoundation.Core` namespace; `CoreApiVersion` major-version gate and minimum-version checks. |

### Core API entry point

`FoundationCoreApi.Core` has `Modules`, `Dispatcher`, `Services`, `Content`, `Events`, `Patches`, `Plugins`, `Diagnostics`, and `ApiVersion`. It is available even before the companion plugin's `Awake`; modules registered early will activate once the Core component begins updating after BepInEx finishes initialization.

### Mod setup

1. Add `[BepInDependency("urakka.kingdominc.foundation.core")]` to your own BepInEx plugin.
2. Implement `IFoundationModule` and optionally `IFoundationWorldModule`.
3. In your plugin's `Awake`, call `FoundationCoreApi.Core.Modules.Register(module)`.
4. Retain the returned registration handle and dispose it in your plugin's `OnDestroy`.
5. Register cross-mod services, event listeners and Harmony patch classes using the **owned** `IModContext` passed to `Initialize`.
6. Continue using existing `FoundationApi` for research, machines, IO, settings, etc.

See `Examples/CoreMod` for one file per class/interface.

### Main-thread dispatch from background work

Do not access Unity objects or native `GamePlayer` properties from an HTTP callback or task thread. Enqueue the operation instead:

```csharp
context.Dispatcher.Post(() =>
{
    context.Dispatcher.RequireMainThread();
    // Safe to inspect/modify native state using FoundationApi here.
});
```

The token is tracked by the module scope: unloading the mod cancels work that has not yet run. Exceptions in one queued action are logged and do not stop other modules.

### Ownership rules

- Service ID: `your.mod.guid:service-name`, registered with a shared C# interface type, or exposed by Type-based lookup.
- Content ID: `your.mod.guid:content-name`, registered for catalog discovery; underlying native content must still be registered through native Foundation APIs.
- Harmony patches get `urakka.kingdominc.foundation.core.<mod-id>` as their owner. Disposing one module never unpatches another module.
- `CoreLifetime.World` services are removed on kingdom changes. Module-lifetime services are removed on module shutdown.
- `ContentLifetime.NativePermanent` is process-lifetime: disposing a mod cannot remove an item ID that a save might contain. `OwnerActive=false` indicates an unavailable provider.
- Any failure in another module's event callback or world lifecycle callback is logged but does not terminate other subscribers or prevent the game's own operation.

### Limitations

- Unity Mono/BepInEx 5 cannot safely unload a managed assembly and remove native game item/technology registrations during a running process. Managed resources can be cleaned up; native content stays until process exit.
- Actual native gameplay remains the responsibility of the 0.10.1 APIs. Core infrastructure only manages registrations and sharing.
- Harmony overlap inspection finds shared patch targets; it cannot guarantee arbitrary third-party patches commute.
- This source overlay has not been compiled against the user's installed Windows game in this container. Do not treat the prior 0.9.3 audit as proof that this new Core component is runtime verified.

## Source organization

`src/Foundation/` has already been organized into domains: `Core`, `Bootstrap`, `Research`, `Workspace`, `Gameplay`, `Signals`, `UI`, `Persistence`, `Updates` and supporting contracts. Each file has **one top-level C# type**. Nested Harmony patches remain attached to their original partial parent type to preserve private member access and Harmony patch identities.

The single `Foundation.csproj` compiles `src/Foundation/**/*.cs`. Neither a source organizer nor a `Directory.Build.targets` compatibility shim is needed. All tools are under `Tools/`, the single root command is `INSTALL-FOUNDATION.cmd`, and documentation is under `Docs/` and `SDK/`.

