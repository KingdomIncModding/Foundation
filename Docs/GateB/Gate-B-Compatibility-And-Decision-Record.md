# Foundation 0.11.0 — Gate B compatibility and decision record

**Date:** 2026-11-09. **Baseline:** `Foundation-0.10.6-Gate-A-Native-API-Audit-Complete-Source.zip` and its native API freeze-candidate source hash manifest. **Working source:** the updated `Foundation-0.11.0-Stage-B-Full-Test-Runner-F8-Fix-Complete-Source.zip` plus the Gate B audit corrections included in this package.

## 1. Decision and meaning of "passed"

The user authorized proceeding as though prior development tests passed. That is **provisional development acceptance**, not a representation that the 0.11.0 runtime build, old dependent mods, missing-content save restoration, or external Harmony conflicts were independently tested. **Gate B has a complete SOURCE audit and migration plan; final compatibility certification is withheld until reproducible runtime evidence is archived.** This distinction is mandatory under the controlling Foundation 1.0 roadmap.

Stage B is the *Foundation-created layer*. It must use the verified native Game API rather than duplicate the game's item pipes, crafting, research, combat, workspace, or other authoritative state. Game-owned `FlowPipe` connections and mod-defined logical signals remain separate. A new owner-facing helper is not evidence that the underlying native behavior works.

## 2. Exact production-source diff

The diff generator checked the **actual ZIP baseline**, all 396 frozen original C# files, and the current tree, not merely a README's file counts. Output: `Gate-B-Source-Compatibility-Audit.json`. Baseline production C# files: **396**. Current: **407**. **11 added, 23 modified, 0 deleted, 373 unchanged**. There were **no changes to the native intrinsic Gameplay/Machines/IO implementation paths**; the single changed gameplay-path file, `Gameplay/GameContentRegistry.cs`, is specifically the Foundation-owned content registration bridge, not the game simulation. All 396 original file hashes match the frozen baseline manifest. See the machine-readable audit for file names and SHA256 fingerprints.

This is source-structural preservation, **not** proof of binary compatibility, load/save data survival, native correctness, or independent third-party behavior.

## 3. All 24 rewrite candidates — source traceability

`Stage-B-24-System-Traceability.csv` maps each `R01–R24` to exact implementation files and associated existing smoke-test case IDs. A row marked **mapped** means a reviewed implementation source and a test entry exist; it does **not** mean its rewrite is demonstrably feature-complete in-game. The following categories retain additional validation dependencies:

- **B1 (R01, R02, R03, R04, R06, R07):** one authoritative BepInEx/Core directory, world lifecycle, service ownership/versioning, shared Core/legacy event delivery. Must test with **two real independently compiled consumer mods**, including provider unload/reload and one deliberately throwing handler.
- **B2 (R20, R21, R22):** owner-scoped Unity assets, native content IDs, save schema. Need a disposable kingdom with real pre-load content registration, native game save/reload, missing provider retention, asset instantiation and release.
- **B3 (R08–R11):** signal source/property selection, exact type rules, link persistence, native player/workspace/camera read-only adapters. Need a native-world signal provider returning/changing actual values and restoration when a provider disappears and returns.
- **B4 (R12, R13, R15):** native-themed Options and machine-specific UI; must inspect actual button, toggle, slider, dropdown, text/number input, layouts at multiple resolutions, Apply/Cancel/save, keyboard focus and owner cleanup. Stage B's visually rendered test buttons are temporary test controls, not a substitute for production panels.
- **B5 (R16–R18):** developer console, native-state inspector and diagnostics. F8 is intentionally disabled by default, enabled only during the full-suite Stage B session, and the script restores the user's config. Confirm actual F8 opening and each inspector tab.
- **B6 (R05, R14, R19, R23, R24):** user explicitly requested **REWRITE ALL FIVE without removing functionality**, overriding the roadmap's "review first" default. See the five explicit decisions below.

## 4. B6 decisions: ALL REWRITE, no feature removal

| ID | Decision | Source bridge used | Pre-existing functionality retained / how to migrate | Runtime evidence outstanding |
|---|---|---|---|---|
| **R05 Harmony Patch Manager** | **REWRITE** | `CorePatchManager`, `CorePatchSession`, owner-scoped `ICorePatchManager`; Harmony remains the native patch implementation mechanism | Preserve `PatchClass(ownerId, patchType)`, `Conflicts(ownerId)`, `ReleaseOwner(ownerId)`, stable Harmony namespace and scoped cleanup; no patch-all global unpatch | Two independent real mods patching the same harmless native method; partial patch failure and owner removal |
| **R14 ModBarContext** | **REWRITE** | `FoundationApi.NativeUiFactory` + existing game HUD and Foundation theme | Keep `IModBarContext.Root/CreateText/CreateButton/CreateDropdown/CreateToggle/CreateSeparator`, callbacks, owner tokens and native layout; do not invent a second Canvas | Actual mod bar rendered in a kingdom; pointer/keyboard, narrow layout, lifecycle cleanup |
| **R19 Verification System** | **REWRITE** | `Tests/StageB` and `Tests/FullSuite`; installed `Foundation.dll` version and SHA checks | Keep older A1/A2/A3–A7/B1 independent test scripts and one-shot behavior; no gameplay mutation or continuous polling; only one verifier installed at once | Windows build; test runner with latest archive; report freshness, F8 config restoration, severe-FPS regression check |
| **R23 Missing Research Recovery** | **REWRITE** | `FoundationApi.Progression.FindTech`, `FoundationApi.ResearchRecovery`, existing `MissingResearchRecovery` save shim | Preserve missing native `Player.Tech`, `Player.NewTechCount`, source tech IDs, unknown notifications, and native load-failure recovery; no fake tech IDs or own authoritative research graph | Remove/re-add a real registered tech mod on a **copy** of a kingdom, save/reload and compare both native JSON and game-visible research |
| **R24 Workspace Node Registry** | **REWRITE** | `FoundationApi.Progression.FindTech`, `FoundationApi.Presentation.FindSprite`, existing `WorkspaceNodeRegistry` catalog | Preserve `RegisterWorkspaceNode`, `TryGet`, `Nodes`, `Changed`, ID/factory, native FlowElement placement, IO and persistence; do not create a duplicate authoritative world registry | Register, place, connect, save, reload and remove/re-add a real mod-defined workspace node on a disposable save |

**No public B6 entry points were intentionally removed** in the compared source. This is a meaningful source contract finding, but runtime verification against existing external mods is still required.

## 5. ABI and data-compatibility warning — important for other mod authors

**HIGH: `IFoundationCore` acquired two interface members in B1:**

```csharp
ICoreModDirectory Mods { get; }
ICoreServiceDirectory ServiceDirectory { get; }
```

The baseline interface did **not** have those getters. Adding required members to an existing CLR interface can break **third-party classes that implement `IFoundationCore`**, even without removing an old method. The interface name remains the same, so a simple source-level "no deleted declarations" audit would **not** catch the binary break. Existing mods that only **consume** the factory `FoundationCoreApi.Core` are a different scenario; their compatibility should still be verified with actual compiled DLLs.

**Immediate migration for implementers:** adapt the concrete class to implement both new getters with real Core services (`ICoreModDirectory` and `ICoreServiceDirectory`); do not return a fake secondary directory. Rebuild and test against the installed Foundation version. **For most mod authors:** don't implement the framework-owned `IFoundationCore`; consume `FoundationCoreApi.Core` and use `Modules/Services/Events`, plus the new `.Mods` and `.ServiceDirectory` when appropriate. Do **not** change an existing mod's BepInEx GUID, native item/research IDs, workspace node IDs, signal IDs or save schema keys.

**Release blocker:** Until legacy interface-implementer binary tests run (or the API is redesigned with a separately versioned non-breaking extension interface and consumer migration), Foundation 1.0 must **not** promise unconditional binary compatibility for such implementations. No existing third-party mod DLLs were provided for this package.

Other source-level retention findings: `FoundationApi` older static API properties/methods remain present in the source; the newer owner-scoped APIs (`AssetDirectory`, `SignalDirectory`, `SignalLinks`, `SaveMigrations`, `ContentDirectory`, `NativeUiFactory`, `ResearchRecovery`) are layered over existing shared registries. The game-side native source implementation is unchanged. A byte-level save comparison, API ABI scan from the *compiled* assemblies, and actual consumer mod tests are still required for release signoff.

## 6. Gate B acceptance checklist with evidence honesty

| Roadmap acceptance clause | Source audit | Live proof status |
|---|---|---|
| 24 systems mapped; five B6 explicit REWRITE decisions | ✅ Mapped, all decisions recorded | Runtime for individual rewrites remains unverified |
| Native gameplay remains authoritative; no duplicate simulation | ✅ Native intrinsic C# files unchanged; wrappers mapped | Requires game-state behavior verification |
| Stable IDs, lifetime, cleanup, ownership and missing-provider semantics | ✅ API and tests documented | Save/asset/provider fixtures unverified |
| Core cross-mod lifecycle, service and events | ✅ Source checks | User-reported earlier B1 PASS; no archived 0.11.0 two-mod fixture |
| Signals separate from FlowPipe | ✅ Source contract | Live persistence/removal fixture unverified |
| Native-themed machine/mod UI | ✅ UI source checks; smoke GUI exists | User accepted earlier screenshots, but current 0.11.0 visual roundtrip unverified |
| Console / inspector / diagnostics use Game API | ✅ Source mapping and F8 runner repair | Actual F8/UI interaction not independently archived |
| Old tech/workspace save data restored | ✅ Existing ID handling and source shim retained | **Critical** native save/reload fixture unverified |
| Startup, lifecycle, save, native operations, cleanup, external consumers | ✅ Independent test scripts available | Several real-world test scenarios and Windows compile remain unverified |

**Gate B outcome: source traceability PASS; formal compatibility/release gate CONDITIONAL (NOT independently certified).** The user's "assume passed" decision authorizes **continued development**, not fabricated runtime logs or unconditional preservation claims.

## 7. Full-suite runner repairs included in this complete package

- Correct PowerShell 5.1 literal `$script:abort=$false` (was `$script:abort=false`, resulting in `The term 'false' is not recognized`).
- Sanitize nested script names such as `FullSuite/verify_full_suite_runner.py` into a valid single log filename.
- Reject verifier reports that predate the installed test DLL or the current game session. Reject stale visual reports; do not upgrade B4/B6 to PASS from old interactive JSON.
- Recover original `[Developer] EnableConsole` settings **before** overwriting `latest.txt` at the start of a new run.
- Reject unknown visual status strings, and only count positive interactions performed in the fresh current test session.

Run **`Tests/GateB/GATE-B-AUDIT.cmd`** for read-only static checks, or **`RUN-ALL-TESTS.cmd`** for sequential game-backed A1–A7 and B1–B6 verification. Stop and remove temporary test DLLs after use to avoid the previously observed FPS regression. Neither script creates game content or overwrites player kingdoms automatically.

## 8. Strict instructions for the follow-up Foundation 1.0 roadmap audit

Read **every section** of `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md` (source inventory, A1–A7, Gate A, B1–B6, Gate B, final 1.0 work), **do not use the old priority order**, and convert every stated operation and exit condition into an evidence-backed row. Reconcile the user-provisionally-accepted runtime checks with any still-pending real-world scenarios. Do not mark 100% on the basis of an API method signature or a one-shot status alone. In particular preserve all current mod functions and document any breaking change needed for external mods to migrate.
