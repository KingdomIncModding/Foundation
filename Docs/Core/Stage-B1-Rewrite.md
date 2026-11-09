# Foundation 0.10.7 — Stage B1 Core Framework and Inter-Mod Infrastructure

**Source baseline:** `Foundation-0.10.6-Gate-A-Native-API-Audit-Complete-Source.zip`, itself based on user source `Foundation-Source-20261109-125339-96756.zip`.

**Gate A decision:** The project owner explicitly directed us to **assume Gate A passed and proceed**. This authorizes Stage B work; it is a project scheduling decision, **not** retrospective proof of all previously undocumented operation-level runtime tests. The original Gate A freeze candidate under `Docs/GameAPI/GateA` is intentionally preserved unchanged as the immutable native baseline.

**Release scope:** All **six B1 rewrite candidates** R01/R02/R03/R04/R06/R07 are addressed in this single complete source revision. No Stage B2–B6 rewrite or native transport/gameplay modification is included.

## B1 source changes and acceptance

| Roadmap item | Implementation | What it does | Evidence |
| --- | --- | --- | --- |
| **R01 Mod Registry** | `Core/Runtime/CoreModDirectory.cs`; `Core/Contracts/ICoreModDirectory.cs`; `Core/Models/ModDirectoryEntry.cs`; existing `CoreModuleRegistry` and `CorePluginCatalog` | Exposes a **single live discovery view** of BepInEx plugins and Foundation module registrations, keyed by their stable ID, with live status and metadata. **No second authoritative dictionary**, no parallel registration lifecycle. | Stage B1 static boundary checks and runtime checks `B1.LiveModDirectory`, `B1.ModDirectoryMerged`. |
| **R02 Mod Lifecycle** | `Core/Modules/CoreModuleRegistry.cs`; `ModRegistration.cs` | Observes world changes before late module activation; freezes the registered module descriptor; properly stops dependents on provider removal, releases owner-scoped services and subscriptions, and allows reactivation when provider is registered again. Prevents recursive unregister while already Stopping. | `B1.ProviderRemoval`, `B1.ProviderRecovery`, `B1.ModuleCleanup`; existing native save-load observation remains untouched. |
| **R03 Dependency Manager** | Existing `ModuleDependencyResolver.cs`, `ModDescriptor`, `ModDependency` retained; registry integration hardened | Existing required/optional BepInEx and Core dependencies, minimum versions, deterministic activation and cycle checks preserved. New recursive dependency closure handles transitive provider unload/reload without permanently unregistering consumers. | `B1.DependencyWait`, `B1.DependencyActivation`, `B1.DependencyCycle`, `B1.ProviderRecovery`. |
| **R04 Service Registry** | `Core/Services/CoreServiceRegistry.cs`; new `Core/Contracts/ICoreServiceDirectory.cs` | Existing type+service-ID registry remains authoritative. Adds discovery by contract/version and by owner, and unambiguous provider+contract lookup, with original lifetime and disposal semantics. | `B1.CrossModResolve`, `B1.VersionRejection`, `B1.ProviderRemoval`, `B1.ModuleCleanup`. |
| **R06 Event System** | `Api/FoundationEventBus.cs`; `Integration/Nested/Entry.cs`; `Core/Events/CoreEventSubscription.cs` | **Converges legacy `FoundationApi.Events` and `FoundationCoreApi.Core.Events` delivery onto the existing `CoreEventBus`**. Legacy facade retains its public API and ownership/token metadata, but does not dispatch a second callback stream. Subscription disposal uses atomic state. | `B1.OneEventBus`, `B1.EventTokenCleanup`, `B1.SubscriberIsolation`. |
| **R07 Inter-Mod Communication** | `IFoundationCore.Mods`, `IFoundationCore.ServiceDirectory`, same Core bus, scoped mod contexts | A consumer discovers providers and their shared C# contracts without referencing the provider **implementation**. Core/BepInEx IDs and service owner IDs remain distinct concepts. | One-shot runtime consumer in `Tests/StageB1`; use `core.ServiceDirectory.Find(typeof(ISharedInterface))`. |

### Public API (additive, not replacing existing members)

```csharp
var core = FoundationCoreApi.Core;

// Live directory: includes installed BepInEx plugins and Foundation module registrations.
var mods = core.Mods.Entries;
if (core.Mods.TryGet("example.foundation.core", out var provider))
{
    var displayedName = provider.Name;
    var active = provider.Module?.State == ModState.Active;
}

// Inter-mod service discovery using a shared interface contract.
// Provider registers: scope.Services.Register<IExampleCounter>(
//     "example.foundation.core:counter", instance, new Version(1, 0));
var compatible = core.ServiceDirectory.Find(typeof(IExampleCounter), new Version(1, 0));
if (core.ServiceDirectory.TryResolveProvider<IExampleCounter>("example.foundation.core", out var counter))
    counter.Increment();

// If a provider registers multiple services under the same interface,
// TryResolveProvider returns false: use Find + the existing exact-ID resolve API.
foreach (var service in compatible)
    if (core.Services.TryResolve<IExampleCounter>(service.Id, out var selected)) { /* inspect */ }

// The legacy and Core event contracts now deliver through one shared bus.
using (var token = FoundationApi.Events.Subscribe<ExampleEvent>("example.consumer", OnEvent))
    core.Events.Publish(new ExampleEvent());
```

`IExampleCounter`, `ExampleEvent`, and `OnEvent` in this snippet are illustrative **consumer-owned types** (not new Foundation or game classes). The actual SDK example contract is `Examples/CoreMod/IExampleCounter.cs`. Cross-mod consumers should reference a shared **contract assembly**, not the other mod's implementation DLL. A caller cannot safely discover unknown C# interfaces without explicitly agreeing on their type.

### Compatibility and ownership

- The production assembly is still a **single `Foundation.dll`**. The existing root `INSTALL-FOUNDATION.cmd`, API and BepInEx component IDs are preserved.
- Native Game API A1–A7 files, machine telemetry/pipe graph, all UI panels/theme, Signals, content registration, custom saves and the unrelated B6 review items are **untouched**. `FoundationPlugin.Version` and `FoundationCorePlugin` BepInEx metadata are updated to 0.10.7.
- The new discovery interfaces extend `IFoundationCore` with additive members. External code *implementing* the `IFoundationCore` interface itself (rather than reading `FoundationCoreApi.Core`) will need to implement the two new properties when rebuilt. No existing entry-point method is removed.
- `ModDirectoryEntry` is a **snapshot**. Read `core.Mods.Entries` again after module or plugin changes. The live BepInEx catalog and Core registry remain the sole sources of truth. Legacy research categories (`TechMods`) stay in Stage B2 content infrastructure and are not mistaken for loaded BepInEx plugins.
- Removing a module **stops but does not unregister its required dependents**; they wait for reactivation. Only their own registration handles unregister them. Services, event handlers, dispatcher jobs and patches owned by stopped modules are released safely.
- World-lifetime registrations are cleaned up using existing `GamePlayer` transitions; no duplicate World/Lifecycle component or native save state is introduced.
- The legacy `FoundationApi.Events.RemoveOwner` removes **facade-created** subscriptions. Scoped Core module cleanup remains owned by the Core registry; consumers must dispose their own tokens. This avoids deleting another module's listeners unexpectedly.
- No native game state is simulated, no signal-vs-pipe crossover, and no new hot-unload of game-native content is attempted. BepInEx 5/Mono does not offer true in-process managed DLL unloading.

## Source verification vs runtime acceptance

Source-level checks: `python Tests/verify_stage_b1.py` (Gate A source hash boundary + B1 invariants), `python Tests/verify_source.py`, existing A1–A7 checks, UI/theme/smoke checks. **These are static checks, not installed game test claims.**

Windows game/SDK build: close Kingdom Inc and run `Foundation\INSTALL-FOUNDATION.cmd`. It uses the installed Kingdom Inc game DLLs and BepInEx and builds the packaged SDK examples. The one-time B1 verifier (development only) is under `Tests\StageB1\STAGE-B1-TEST.cmd`. Use menu option **2** to build/install, launch the game for a few seconds, exit, option **3** to read the report (with Foundation hash/version validation), and option **4** to remove the standalone verifier DLL. The B1 verifier is **one-shot**: no `Update()` polling, no native-world mutation, and only one JSON report write, so it avoids the prior continuous test overhead.

**Remaining acceptance:** Windows build and B1 runtime report, third-party mod consumer compatibility if any, disposal/re-registration scenarios in real BepInEx, and user approval. Do not label B1 100% runtime-verified on a source-only archive.

## Next roadmap stage

After B1 runtime verification, **B2** consists of R20 Asset Registry, R21 Content Registration SDK, and R22 Mod Persistence System, based on native Game API and the B1 ownership/service contracts. B3 Signals, B4 UI, B5 tooling, and B6 five mandated reviews follow the controlling roadmap; they are not started by this package.
