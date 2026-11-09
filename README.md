# Foundation 0.10.7 — Stage B1 Core Framework Rewrite

**Game API Stage A (A1–A7):** project-owner-accepted Gate A assumption; the Gate A source-freeze candidate and original audit evidence are kept unchanged. This release addresses the six **B1** custom-system candidates R01/R02/R03/R04/R06/R07 only. No native gameplay or later B2–B6 rewrite is included.

See `Docs/Core/Stage-B1-Rewrite.md` for full API, preservation and acceptance details, and `Docs/Core/Stage-B1-Runtime-Test-Guide.md` for the independent **one-shot** verifier. The installer still produces **one production Foundation.dll**. This package is source-verified; installed Windows compilation and B1 runtime tests remain to be performed.

---

# Foundation 0.10.6 — Stage A1 Native Machine API Full Source

**One complete source archive; no incremental overlays.** This revision builds
on the user-reported verified A2 runtime baseline and the A3–A7 source-complete
Foundation 0.10.5 package. The only new scope is **native game API A1**.

**Native A1 coverage:** 61 concrete original game `FlowElement` classes are
explicitly mapped. The source now exposes per-machine native observations,
inherited native crafting/resource operations, a typed native class catalog,
selected machine-specific configuration bridges, native costs and independent
read-only verification tooling. It does **not** reimplement game machines or
rewrite custom Foundation components ahead of Gate A.

**Read:** `Docs/GameAPI/A1-Native-Machine-Coverage.md`,
`Docs/GameAPI/A1-Machine-SDK-Guide.md`, and
`Docs/GameAPI/A1-Source-Completion-Notes.md`.

**Offline tests:** `python Tests/verify_native_machine_a1.py --native-zip KingdomIncSrc.zip`
(using your own original game source ZIP). **Windows and game runtime acceptance**
are required after this source phase: `Tests/NativeMachines/INSTALL-A1-TEST.cmd`
then `MACHINE-A1-TEST.cmd` option **3**. **No A1 runtime PASS is claimed here.**

Only one production `Foundation.dll` is built. Any verifier DLL is dev-only.

---

# Foundation 0.10.5 — Native Game API A3–A7 Complete Source Candidate

**One complete source package — not an incremental overlay.** This revision is
based on the source-complete Foundation 0.10.4 native IO milestone, which the
user subsequently reported passed its installed runtime checks. The controlling
order is `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`: native Game API
Stage A before any planned Foundation-created Stage B rewrites.

**What changed:** typed game-backed item/equipment/recipe/resource/economy,
research/adventure/combat, workspace editing, native game UI/input/camera/assets,
and actual game-origin event coverage. No duplicated native game simulation,
no fictional game capabilities, no replacement item transport. Existing
Foundation custom systems are maintained for compatibility and left unrewritten.

**Documentation:**

- `Docs/GameAPI/A3-A7-Native-Game-API.md` — native source evidence, contracts,
  semantics, restrictions, and installed test guide.
- `Docs/GameAPI/A3-A7-Stage-Status-Matrix.md` — evidence and remaining runtime
  requirements for each A3–A7 milestone.
- `Tests/verify_game_api_a3_a7.py` — offline source signature and semantic
  checks against the game's extracted source; not a C# compile test.
- `Tests/NativeGameApi/INSTALL-A3-A7-TEST.cmd` — compile/install the complete
  Foundation and independent **development-only** verification plugin against
  actual installed game DLLs. Run in a disposable save.
- `Tests/NativeGameApi/GAME-API-TEST.cmd` — preflight, build, read exact
  PASS/FAIL/PENDING scenarios and remove the development verifier.

**Status:** source additions and static checks completed. **Windows
compilation and in-game A3–A7 test results are still pending.** Do not mark
100% accepted, or check Gate A, based on the ZIP alone. A1 native machine
specialization coverage has its own outstanding roadmap requirement. The
production output remains **one** `Foundation.dll`.

---

# Foundation 0.10.4 — Native Machine IO & Throughput Source Candidate

**Latest complete source baseline:** `Foundation-Source-20261109-094126-36516.zip`.
**Stage:** A2 Native Machine IO & Throughput, under `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`.
**Runtime status:** Unity/BepInEx build and interactive A2 verification pending on installed Kingdom Inc. No custom framework rewrites.

See `Docs/GameAPI/A2-Native-Machine-IO-Throughput.md` for actual native mechanics, API usage, source evidence and the full independent test procedure. This tree is the **complete Foundation source**, not an incremental overlay. One production `Foundation.dll`; a separate optional development-only BepInEx verification consumer is located in `Tests/NativeIO/` and should be removed after verification.

# Previous milestone: Foundation 0.10.3 UI Framework notes — Unified Core, Machines and UI

This is a **source-level merge** of the complete Foundation source uploaded as `Foundation(1).zip` and its embedded `Foundation_Machine_Framework_1.0.5` implementation. **One game-facing DLL** is produced: `Foundation.dll`. The existing Foundation and Foundation Core plugin registrations remain inside it for compatibility, but there is **no standalone Machine plugin**.

## Architecture

- Existing Foundation namespaces, modules, Core interfaces, and API contracts are kept.
- `KingdomFoundation.Machines.FoundationMachineApi` and its public types are **now defined inside `Foundation.dll`**, with the same namespace/type names.
- The standalone `FoundationMachinePlugin : BaseUnityPlugin` has been retired; `FoundationPlugin` owns startup, native-transfer Harmony patch, per-frame polling, and cleanup. **The original Core companion BepInEx entry remains in the same assembly for compatibility with mods depending on the Core plugin ID; no new Machine entry is created.**
- The Machine scanner still polls at 0.25 seconds and the native transfer postfix remains observe-only.
- No vanilla game logic is rewritten.

## Install / build (Windows)

1. Exit Kingdom Inc.
2. Extract this archive to `KingdomInc\_Mods\Foundation` (or another **fresh** folder; pass `-GameRoot` explicitly).
3. Run `INSTALL-FOUNDATION.cmd -UseInstalledBepInExOnly` (or double-click `INSTALL-FOUNDATION.cmd` if BepInEx is already configured).
4. The normal installer builds **using the actual installed Unity/game/BepInEx assemblies**, builds SDK examples, creates the player/SDK ZIPs, then installs one DLL to `BepInEx\plugins\Foundation\Foundation.dll`.
5. After the new DLL has been installed and hash-verified, the installer backs up and removes the two **specific old binaries** `Foundation.Machines.dll` and `Foundation.MachineVerification.dll` (if present), preventing duplicate patches. It does not delete unrelated plugin files or saves.

**Build without installing:** `INSTALL-FOUNDATION.cmd -BuildOnly -UseInstalledBepInExOnly` (no plugin cleanup). This is recommended before deployment.

## Verification after merge

The unified **0.10.2** baseline passed 296 native smoke + 24 independent Core + 26 independent Machine checks on the user's Windows installation. The previous **0.10.3** UI screenshot harness ran in Kingdom Inc (36/37 tests, with the tooltip probe defect since source-corrected). The **new asset-based theme code in this package is not yet compiled or screenshot-verified**. Do not mistake the earlier test run for verification of this revision.

1. Run the built-in Foundation native isolated smoke workflow when safe, keeping backups. Build+Install first; `-Verify` is an optional special mode that needs prerequisites (see `Tools/Development`).
2. To rebuild the independent machine verifier against the merged assembly, run `Tests\INSTALL-UNIFIED-MACHINE-TEST.cmd` (development only). This creates a second DLL temporarily **for testing**, not for players.
3. In-game: enter a disposable kingdom, connect machines, let resources transfer, add/change/remove a machine, return to menu, reload, return to menu, exit.
4. Run `Tests\READ-MACHINE-REPORT.ps1` for fresh 26/26 status. Delete the verification plugin DLL after successful testing. Rerun independent Core verification against the new DLL too.

## Compatibility notice

**Existing mods compiled with an AssemblyRef to `Foundation.Machines.dll` must be rebuilt to reference `Foundation.dll`**. Keeping the namespace/type names alone does not preserve CLR type identity across an assembly merge. Don't ship the old `Foundation.Machines.dll` alongside the merged build to make those mods work; doing so can cause duplicate plugins and conflicting types.

The regular package contains only Foundation.dll. Developer tools may compile independent test DLLs but are not distributed to players.

## Build status

- Core + Machine unified 0.10.2 runtime regression tests: passed previously.
- Prior UI 0.10.3 gallery and screenshot audit: executed in game (36/37 original cases).
- This revision: game-asset-native theme source checks and contrast tests passed.
- This theme revision's Windows compilation, in-game screenshots and visual approval: **not yet run**.
- Native mutation/save/reload hardening remains a separate coverage requirement.

## Complete UI framework source (0.10.3 candidate)

The full UI implementation extends the verified 0.10.2 Core/Machine baseline. It
adds owner-scoped native panels, per-machine editors, dynamic properties,
searchable selection, signal source/property lookup, input fields, tabs,
collapsible sections, confirm dialogs, tooltips, draggable layouts, color swatches,
and live telemetry. Existing ModConfig now has staged string, numeric and
search controls. All production code compiles into the one `Foundation.dll`.

**Read `Docs/UI/README.md` before building.** New source has passed static checks
but has **not** yet been compiled or tested in Kingdom Inc; do not overwrite a
known-good install without backing it up. `Examples/UIDemo` is development-only.

## Canonical Kingdom Inc UI theme / asset snapshot

**The user's extracted `Assets.zip` is the immutable UI art reference until a
Kingdom Inc game update.** This source release includes the asset SHA256 and
native sprite names / nine-slice measurements in
`Docs/UI/Game-Asset-Theme-Snapshot.json`, and the full visual QA acceptance
checklist in `Docs/UI/Game-Theme-Contract.md`. The game loads its own textures;
no raw or redistributed game art is bundled into the production mod.

The theme pass replaces the previous custom gallery's jarring green machine
buttons and fields with the game's actual Options button/ornament art wherever
loaded, with measured plum/cream/brass fallbacks. Native Options Mod Settings
keep using the original template styles. The optional UISmoke verifier now
reports potential text overlaps and font-sizing issues in `VisualIssues[]`.

**Retest after applying this source revision:** UI-FRAMEWORK-TEST.cmd option 2
(build/install Foundation), option 7 (rebuild UI screenshot verifier), in game
click UI Smoke + Screenshots, option 8 (ZIP report). Inspect every control PNG
against the native Options screen. Do not call the new theme visually complete
until a new screenshot review shows no layout, clipping or contrast faults.

## UI screenshot visual review (2026-11-09 09:07)

The latest 37/37 smoke-result screenshots revealed a Mod Settings Signal Source
caption overflow not caught by the previous geometry analyzer. Source/hotfix
notes are in `Docs/UI/UISmoke-Visual-Review-20261109-090724.md`. The runtime
visual recheck, broader machine coverage, and narrow-resolution tests are still
pending. The target remains one production `Foundation.dll`.


## Four-issue UI visual correction (2026-11-09)

See `Docs/UI/Visual-Regression-Fixes-20261109-4-Defects.md` for the
number/input inset fields, signal captions, checkbox scaling and extended
screenshot-smoke visual failures. The 0.10.3 source requires a new Windows
build and Unity screenshot pass before being considered visually accepted.


## Gate A native Game API audit — current 0.10.6 user-exported source

See `Docs/GameAPI/GateA/README.md` for the complete Stage 0/A1–A7 source inventory,
61 exact native machine declarations, 283 typed API members, 37 classified public
entrypoints and the frozen 24-item Stage B candidate inventory. Gate A source
checks are reproducible with `Tests/GateA/verify_gate_a.py`. The audit has **not**
rewritten any of the Stage B systems or claimed an installed 1.0 freeze.
The independent A1–A7 verifier PASS results are user-reported; installed assembly
hashes and raw reports were not supplied with this source export. The read-only
`Tests/GateA/GATE-A-AUDIT.cmd` captures any still-present evidence without
installing test mods or repeating the overlapping audits that caused poor FPS.

## Foundation 0.11.0 — Stage B integration and B6 compatibility-preserving rewrite

See [Stage B workstream inventory](Docs/StageB/README.md), [Mod author migration guide](Docs/StageB/Mod-Migration-Guide.md) and [independent acceptance checklist](Docs/StageB/StageB-Acceptance-Checklist.md). All five B6 candidates are explicitly **REWRITE**, with existing public functionality kept available. The new `Tests/StageB/STAGE-B-TEST.cmd` is one-shot and development-only. Windows build and real kingdom verification are pending; static validation does not equate to runtime signoff.

## Stage B visual smoke (0.11.0 test harness correction)

A Stage B registry smoke PASS alone never creates a visible button. The corrected
`Tests/StageB` verifier now registers a real HUD `STAGE B UI TEST` button and
interactive popup for manual verification, with no per-frame test loop. See
`Docs/StageB/StageB-Visual-UI-Test-20261109.md`. The production DLL is unchanged.


## Single full verification launcher (A1–A7 and B1–B6)

Run `RUN-ALL-TESTS.cmd` from the Foundation root. It builds the installed
Foundation, performs source checks, runs one independent verifier at a time
through a separate game session, collects a combined report, and removes each
temporary testing DLL afterward to avoid the earlier FPS degradation.
**F8 is disabled by default**; the runner temporarily enables Developer →
EnableConsole for the Stage B UI test and restores the original config on
exit. For full instructions and remaining manual acceptance fixtures, read
`Tests/FullSuite/README.md`.


## Gate B native API / Stage B integration audit (2026-11-09)

See **`Docs/GateB/README.md`**, the complete **24-system traceability matrix**,
`Docs/GateB/Gate-B-Compatibility-And-Decision-Record.md`, and
`Docs/StageB/Mod-Migration-Guide.md`. All five B6 systems are designated
**REWRITE** without deliberate functionality removal. The full-suite runner
includes safe recovery from prior F8 test failures and a source-only Gate B
regression check. This is a source audit and compatibility handoff, **not**
a claim of independently verified 1.0 runtime/save/ABI compatibility.
