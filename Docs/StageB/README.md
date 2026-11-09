# Foundation Stage B — Consolidation and Compatibility (0.11.0)

**Controlling specification:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`.

**Project-owner decision:** Gate A accepted for development. B1 was independently reported passing, and all five B6 reviews are **REWRITE with no functionality removed**. B6 implementations must use Foundation's public native API or Core-owned adapters wherever possible, preserving old entry points and save data. `Docs/StageB/Mod-Migration-Guide.md` documents downstream changes.

**Build and runtime truth:** this is a **source-level Stage B integration revision**, not a declaration that every Stage B feature is 100% runtime-verified. The release ships a one-shot independent verifier, `Tests/StageB/STAGE-B-TEST.cmd`, and a manual acceptance checklist for tests that cannot be performed safely without a disposable kingdom or a second mod. Every PENDING item is a real release gate; do not convert PENDING to PASS without evidence. No DLL was compiled or installed here (the Kingdom Inc/Unity references and Windows dotnet installation are on the user's machine).

## Workstream status (all 24 candidates)

| ID | Stage | Source contract / implementation | Compatibility assurance | Verification |
| --- | --- | --- | --- | --- |
| R01 | B1 | `CoreModDirectory`, `CorePluginCatalog` | Existing `FoundationCoreApi.Core.Modules/Plugins` retained | B1 independent PASS as reported |
| R02 | B1 | `CoreModuleRegistry`, owner-scoped cleanup | Original registrations and delayed dependencies retained | B1 independent PASS as reported |
| R03 | B1 | `ModuleDependencyResolver` | Existing IDs, optional/required distinctions retained | B1 independent PASS as reported |
| R04 | B1 | `CoreServiceRegistry`, `ICoreServiceDirectory` | `Core.Services` remains authoritative | B1 independent PASS as reported |
| R05 | B6 | `CorePatchManager` + `CorePatchSession` use existing Core public patch facade, owner-scoped Harmony | `core.Patches.PatchClass`, `Conflicts`, `ReleaseOwner` unchanged; Harmony's real patch rules retained | Read-only smoke; real conflict/unpatch pending |
| R06 | B1 | `FoundationEventBus` funnels through `CoreEventBus` | Legacy `FoundationApi.Events` remains a compatible facade | B1 independent PASS as reported |
| R07 | B1 | `CoreModDirectory` and cross-mod service directory | Typed interface and owner IDs, no DLL implementation coupling | B1 independent PASS as reported |
| R08 | B3 | `SignalRegistry` + `FoundationSignalLinks` using live registered properties | Source/property identity, read/write and `Changed` unchanged | One-shot source/route smoke pending Windows |
| R09 | B3 | `SignalRegistry.Find(query,owner)` | Existing UI source selector unaffected | One-shot smoke pending |
| R10 | B3 | `SignalRegistry.TryResolve` and `FindProperties` with exact types | Existing `ISignalSource.TryGetProperty` retained | One-shot smoke pending |
| R11 | B3 | `FoundationNativeSignals` uses Game API read-only values and existing `SignalSourceDefinition` | Native player/workspace/camera IDs unchanged | Inspect actual values with loaded kingdom |
| R12 | B4 | `FoundationApi.ModConfig` and native options integration | Existing section/edit sessions and Apply/Cancel remain | UI visual/interaction audit pending |
| R13 | B4 | `ModConfigTemplates` based on native Options controls | Current configured controls and text/icon theme preserved | UI visual/interaction audit pending |
| R14 | B6 | `ModBarContext`, native control factories via `FoundationApi.NativeUiFactory`, Core-owned context | `IModBarContext` signature and clickable HUD bar unchanged | Read-only registry smoke + real UI/keyboard pending |
| R15 | B4 | `FoundationApi.Panels`, `FoundationModContext.RegisterPanel` (owner tracked) | Existing panel registry, machine panels and theme intact | One-shot owner cleanup + UI visual pending |
| R16 | B5 | `FoundationPlugin.FoundationDeveloperConsole`, disabled by default, uses `FoundationApi` readers | F8 console, all tabs, same config key retained | Manual F8 check pending |
| R17 | B5 | Console workspace inspector reads `FoundationApi.World`, `FoundationApi.Flow`, `FoundationApi.IO` | Existing machine inspection/throughput display retained | Gameplay read-only inspector pending |
| R18 | B5 | `FoundationApi.Diagnostics`, `core.Diagnostics` | Existing history/error reporting, logs and diagnostics retained | One-shot message check pending |
| R19 | B6 | Independent `StageBVerificationPlugin` one-shot smoke, plus existing test scripts | Old A1–A7, B1, UI verification sources kept; no test DLL installed with production | One-shot smoke + optional manual requirements pending |
| R20 | B2 | `FoundationAssets` / `IFoundationAssets.ForOwner` + `ReleaseOwner` and tracked handles | `LoadSprite`, `LoadAssetBundle`, `TryGet`, `Dispose` unchanged | One-shot preflight + Unity sprite/bundle lifecycle pending |
| R21 | B2 | `GameContentRegistry` and `FoundationModContext` native registration, owner lookup | Native content IDs/registries remain authoritative; no unsafe hot-unregister | Registration/save/load in a disposable kingdom pending |
| R22 | B2 | `ModSaveRegistry.RegisterMigrating` (opt-in schema upgrade) | Same `FoundationModData` root, old `Register`, opaque future/missing mod data unchanged | One-shot contract + actual save/load/migration pending |
| R23 | B6 | `MissingResearchRecovery` now queries `FoundationApi.Progression.FindTech`, read-only `FoundationApi.ResearchRecovery` | Original `Tech` and `NewTechCount` retained without creating fake research, load recovery intact | Save with missing technology and reintroduce provider pending |
| R24 | B6 | `WorkspaceNodeRegistry` now resolves native tech/icon via `FoundationApi.Progression`/`Presentation` | Same `IWorkspaceNodeRegistry`, no duplicate node store, same creation/persistence rules | Native placement/save/load/reload pending |

**Release guard:** Do not conclude full B1–B6 acceptance from static checks or a single one-shot smoke test alone. Native content round-trips, research recovery, real UI visuals, cross-mod lifecycle, WorkspaceNode placement, and Harmony conflicts require additional game verification. See `StageB-Acceptance-Checklist.md`.

## Source behavior and design restrictions

1. **No second authoritative game state:** All native content still registers in the game's static catalogs. Native FlowPipe item transport remains game-owned.
2. **No duplicate mod registries:** `FoundationCoreApi.Core` is the B1 service/lifecycle owner. New B3 `SignalLinks` only owns logical connections, not signal properties or native IO.
3. **No breaking removals:** Old interfaces and entry points stay exported and functional. New methods are additive; see interface-implementer warnings in the migration guide.
4. **No implicit signal conversions:** Signal property links require *exactly matching* CLR types, reject read-only destinations, and remain unresolved when a provider disappears. They recover upon re-registration.
5. **No automatic loss of saved mod data:** Unknown mods, malformed/newer schema, and unregistered research IDs are retained. `RegisterMigrating` receives a deep copy, and any exception suppresses writes from that provider while preserving the old payload.
6. **No unsafe native content disposal:** Loaded Sprite/AssetBundle assets may be released; native item/recipe/quest/tech/workspace definitions cannot safely be removed from a running kingdom merely by disposing a module.
7. **One production DLL:** `Foundation.dll`. The test DLL is explicitly installed and removed with a separate command; no runtime verifier is bundled as a production dependency.

## Compatibility-critical boundary

The original `IGameContentRegistry`, `ISignalRegistry`, `IModSaveRegistry`, `IFoundationAssets`, and `IFoundationAsset` interfaces are byte-for-byte preserved. Optional new members live on derived `IGameContentDirectory`, `ISignalDirectory`, `IModSaveMigrations`, `IFoundationAssetDirectory`, and `IFoundationAssetWithDependencies`. Old consumers and third-party interface implementers do not need to implement new members. Runtime ABI compatibility still requires real external mod loading tests. No existing public method was removed or renamed.

## Test commands

Close Kingdom Inc before building. Install `Foundation.dll` using `Foundation/INSTALL-FOUNDATION.cmd`, then `Foundation/Tests/StageB/STAGE-B-TEST.cmd` option 2. Start Kingdom Inc, load a disposable kingdom, exit and choose option 3 to read the one-shot report. The report includes the exact installed `Foundation.dll` SHA256 and version; it distinguishes PASS, FAIL, and PENDING. Choose option 4 to remove the test DLL afterward. **Do not leave A1/A2/A3–A7/B1/UI smoke DLLs active simultaneously**, because repeated verification caused severe game FPS loss earlier.
