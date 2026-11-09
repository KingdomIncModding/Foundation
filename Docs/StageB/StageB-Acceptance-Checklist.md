# Stage B acceptance: 100% means functional equivalence, not source compilation

A development declaration of 100% requires all of the following evidence for the **exact installed Foundation DLL**.

| Acceptance gate | Evidence | Current state in this package |
| --- | --- | --- |
| Windows build of Foundation against installed Kingdom Inc assemblies | `INSTALL-FOUNDATION.cmd` exit 0 and build log, 1 production DLL | **PENDING**: not executable here |
| All 24 candidates mapped to actual source implementation and integration | `StageB/README.md` and Game API coverage matrix | **DOCUMENTED** (implementation breadth still requires independent review) |
| Legacy public entry points remain usable by existing consumer mods | Rebuild two external mods against Foundation, run load/unload | **PENDING** |
| B1 ownership/service/event/dependency regressions | Stage B1 one-shot verifier | **PASS reported by user on 0.10.7; recheck 0.11.0** |
| B2 asset registration and cleanup | Load sprite/bundle, track, attach to native UI, dispose, verify fallback | **PENDING** |
| B2 game content native registration and serialization | Register all native-supported types before kingdom load; save/reload | **PENDING** |
| B2 mod-owned save migration and missing provider retention | Save schema 1, upgrade to schema 2+, remove/re-add provider, rerun | **PENDING** |
| B3 signal property source/destination correctness | One-shot signal routing case plus provider reload and type mismatch | **PENDING** |
| B3 signal persistence and missing-provider restore | Save owner links into `ModSave`, reload without provider, restore provider | **PENDING** |
| B4 UI and config behavior | Native Options Apply/Cancel, selection sources, custom panels, layout | **PENDING** |
| B5 developer console and inspector | F8, all tabs, real machine/pipe/read-only state | **PENDING** |
| B5 diagnostics and low-overhead execution | Isolated exceptions, no crash, no FPS regression | **PENDING** |
| B6 R05 patch conflicts/unpatch | Two independent mods patch the same safe native method; release by owner | **PENDING** |
| B6 R14 bar controls | Native-themed text/button/toggle/dropdown/sep, callbacks, cleanup | **PENDING** |
| B6 R19 independent testing | One-shot report written once, SHA/Version validated, plugin removable | **PENDING** |
| B6 R23 native missing-tech retention | Removed mod content survives load/save and restores when provider returns | **PENDING** |
| B6 R24 workspace node persistence | Custom FlowElement placement, save/reload, connection and icon integrity | **PENDING** |
| Final full regression A1–A7 + B1–B6 | Check reports and clean game startup/fps | **PENDING** |

Do **not** report the entire Stage B as runtime-verified if any row is PENDING or FAILED. Static tests can prove source structure only. The game-native XML/JSON save and native Unity lifetime tests require a disposable kingdom and exact runtime version.

## Safe smoke run

1. Close game and remove old verification DLLs. Take a backup of the active save.
2. Compile/install the unified Foundation DLL.
3. Run `Tests/StageB/STAGE-B-TEST.cmd` option 2, then launch Kingdom Inc normally (no mutation launch flag needed for this one-shot suite).
4. Load a test kingdom, wait for one-shot BepInEx log line, close game.
5. Option 3 reads SHA-validated JSON report and shows PASS/FAIL/PENDING.
6. Option 4 removes test DLL. Re-launch with only production Foundation to check FPS and interaction.
7. Manually finish the pending live-world scenarios and record results alongside screenshot/console logs.

## Stage B visible UI regression (2026-11-09)

The original 20 PASS/7 PENDING report did **not** render buttons. Registration-only
checks disposed a text token and a blank panel. The corrected test is in the same
`Foundation.StageBVerification.dll`. Follow
`StageB-Visual-UI-Test-20261109.md` to exercise the persistent native Mod Bar
button/toggle/dropdown and the real custom popup. Option 3 merges fresh visual
callback evidence for B4.RenderingVisualAudit and B6.ModBarNativeVisual. Other
pending checks are not auto-passed.
