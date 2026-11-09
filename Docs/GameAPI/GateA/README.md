# Foundation 0.10.6 — Gate A native Game API audit

**Controlling plan:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`.
**Input:** `Foundation-Source-20261109-125339-96756.zip` (2026-11-09, user-exported) and the project-supplied `KingdomIncSrc.zip`.
**Scope:** Audit Stage 0 and A1–A7, retain current custom systems without beginning any of the 24 Stage B rewrites/reviews.
**Gate verdict:** **SOURCE CONTRACT BASELINE MAPPED; FORMAL GATE A SIGNOFF PENDING**. This is deliberately stricter than "all smoke tests passed."

## What this audit establishes

- The **current** user-exported source is the baseline, not an older Foundation package. The complete unmodified 0.10.6 production C# source matches its ZIP hash-by-hash; no custom implementations were edited during this audit.
- The native-game source ZIP has the SHA-256 recorded in the A1 native inventory. All **61** concrete mapped native machine declarations match their expected game-source file SHA-256 values; this includes internal/non-buildable classes, which are not misrepresented as playable machines. Native `FlowElement.GetAllTypes()` remains the authority for what is actually registered in-game.
- **37** public `FoundationApi` service properties have explicit Stage A or Stage B ownership assignments; mixed surfaces (`Presentation`, `Saves`) are labelled **BRIDGE+REWRITE**, not incorrectly declared entirely native.
- **283** typed members across **19** direct Game API/custom compatibility interfaces are listed with their actual implementing source files. The audit additionally checks **35 exact native game source-file references** for the 16 native-backed service interfaces. These are implementation declarations/source provenance, not proof that all 283 have been independently invoked in Unity.
- Existing A1, A2, A3–A7 source contract suites independently checked the mapped native source and reported 84, 35, and 138 passes respectively. The new Gate A structural/source audit reports **421 PASS, 0 FAIL** when given both the current user-exported ZIP and the supplied game-source ZIP.
- All **24** future custom-system candidates remain unchanged: **19 planned rewrites, 5 mandatory KEEP/REWRITE/MERGE/REMOVE reviews**. This is inventory only, not permission to begin B1–B6 before Gate A.

## User-reported installed runtime evidence (not equivalent to independent logs)

Prior conversation reports:

| Stage | Reported result | What is actually established by that statement |
| --- | --- | --- |
| A1 | PASS — all registered native machine types independently inspected | Native machine discovery/inspection for registered/placed types; **does not exercise every mutating machine-specific operation** |
| A2 | PASS — no pending checks remain | Verifier's enumerated native IO/throughput scenarios passed; does not prove unenumerated operations |
| A3–A7 | PASS — user reported all tests passed | Reported verdict for the **55** named verifier checks; actual JSON, game DLL SHA and test conditions not attached to this source export |
| Performance | Normal after old verifier DLLs removed | Supports keeping test DLLs development-only and running them separately |

Additional **A1 action-level gap:** the independent native machine test observes native instances and has four invalid-handle guard calls, but does not prove the success paths of the **26** public `MachineOperations` commands. `Connect` and `Disconnect` have separate A2 verifier coverage. The `Machine-Operation-Runtime-Gaps.csv` records each distinction; absence of successful-action proof is not a claim the implementation is wrong.

The source archive does **not** include the installed `Foundation.dll`, `Assembly-CSharp.dll`, or original unedited live `native-a1-machines.json`, `nativeio-verification.json`, `gameapi-a3-a7.json` reports. This audit therefore records these as **user-reported pass**, not reproduced hash-matched evidence. No Windows game compilation or Unity runtime occurred inside this source-audit environment.

## Current Gate A exit decision

| Required Gate A exit condition | Audit outcome |
| --- | --- |
| A1 — actual types, native properties, operations, IO and restrictions mapped | **SOURCE DOCUMENTED**: 61-type native matrix, registered-type discovery and specializations; independent runtime inspection reported PASS. **Per-operation mutation coverage is not fully evidenced.** |
| A2 — ports, pipes, validation, editing, observed throughput | **SOURCE DOCUMENTED**: real game methods plus A2 35 source checks. User reports independent runtime suite PASS. |
| A3 — item/recipe/resource/economy bridge | **SOURCE DOCUMENTED**: member matrix, native access and source tests. User reports runtime verifier PASS. |
| A4 — research/adventures/combat/progression bridge | **SOURCE DOCUMENTED**: member matrix, native source checks. User reports runtime verifier PASS. |
| A5 — workspace editing/native graph operations | **SOURCE DOCUMENTED**: current bridge methods and source checks. User reports reversible mutation test PASS. |
| A6 — native UI, input, audio, camera, assets | **SOURCE DOCUMENTED**: native-backed methods; custom registration remains frozen for Stage B. User reports runtime verifier PASS. |
| A7 — native event origins and read-only values | **SOURCE DOCUMENTED**: Harmony native event patches and source checks. User reports 55-check verifier PASS (A3–A7 combined). |
| Game assembly build/version, contract freeze provenance | **PARTIAL**: real 0.10.6 source; Windows build reported successful indirectly via later PASS logs, but exact installed game binary SHA, Foundation binary SHA and raw reports not preserved in supplied package. |
| External mod consumer compatibility | **NOT ESTABLISHED**: source compatibility preserved; build scripts compile 4 bundled examples, but external consumers are not enumerated or tested in this audit. |
| No undocumented/untested planned native operation | **NOT ESTABLISHED**: source breadth and independent scenario tests are strong but do not exercise all 283 public typed interface members and all 61 machine-specific actions. |

**Conclusion:** The native API's **source inventory is frozen as a candidate**; do **not** call this an approved 1.0 Game API freeze until runtime provenance and the remaining method/action coverage have been reviewed against the explicit Gate A conditions. Do not automatically expand the game with synthetic functionality to close missing native operations.

## Exact outputs

| File | Meaning |
| --- | --- |
| `Gate-A-Static-Audit.json` | Reproducible source invariant results (does not infer live Unity success). |
| `Public-Entrypoint-Boundaries.csv` | All 37 FoundationApi entrypoints, NATIVE/BRIDGE/REWRITE/REVIEW ownership and Stage. |
| `Native-API-Member-Matrix.csv` | 283 public contract method/property declarations and implementation file evidence. |
| `Native-Machine-Coverage-Matrix.csv` | All 61 exact native classes, original source file hashes and operation/restriction descriptions. |
| `Native-Service-Source-Provenance.csv` | 35 explicit original Kingdom Inc source file references supporting 16 Foundation native API services. |
| `Machine-Operation-Runtime-Gaps.csv` | All 26 native machine mutator methods and exact distinction between A1 guard, A2 transport and successful-operation evidence. |
| `Stage-B-Frozen-Inventory.csv` | R01–R24 actual current source locations; R05/R14/R19/R23/R24 explicitly remain review decisions. |
| `Game-API-Freeze-Candidate-Hashes.json` | Immutable candidate hash set for all production C# files, to detect unwanted changes before Gate A signoff. |

## Reproduction

From the Foundation source root with Python 3:

```bat
python Tests\verify_source.py
python Tests\verify_native_machine_a1.py --native-zip C:\path\to\KingdomIncSrc.zip
python Tests\verify_native_io_a2.py --game-source C:\path\to\extracted\Source
python Tests\verify_game_api_a3_a7.py --game-source C:\path\to\extracted\Source
python Tests\GateA\verify_gate_a.py --native-zip C:\path\to\KingdomIncSrc.zip
python Tests\GateA\export_rewrite_inventory.py
```

The first `verify_gate_a.py` execution for an **explicitly reviewed new baseline** may use `--write-freeze` only when no freeze hash manifest exists. Later runs **must not silently overwrite** the manifest.

On the Windows game machine, **after the game is closed** and old test DLLs have been removed:

```bat
Foundation\Tests\GateA\GATE-A-AUDIT.cmd
```

This script only reads installed hashes and *existing* runtime report files, writing `Foundation\Verification\GateA\gate-a-installed-evidence.json`. It **does not reinstall test mods, repeat heavy audits, mutate a save, or turn on a launch flag**. A1/A2 reports bind to the Foundation DLL/version but did not record a game-assembly SHA, so they are marked `PASS_FOUNDATION_BOUND` rather than fully game-hash-bound if present. If the prior cleanup deleted the reports, it honestly records `MISSING` while retaining the user-reported passes. Do not load all three test DLLs together again: the previous combination produced about 2 FPS.

## Scope restrictions

- No Stage B custom-system code was changed.
- One production `Foundation.dll`; development verifiers remain separate, optional, removable DLLs.
- Native FlowPipe item transport is **not** a signal graph; no invented throughput caps or fake batch transport APIs.
- Custom registries, content registration, mod saves, signal selectors, mod UI and diagnostics are frozen pending Stage B.
- No static-test report can imply that the user's installed game build, save files or external mods were tested in this environment.
