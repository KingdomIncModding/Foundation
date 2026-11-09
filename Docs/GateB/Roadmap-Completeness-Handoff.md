# Foundation 1.0 — Full roadmap-read handoff (not a 1.0 release declaration)

**Primary controlling document:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`, 565 lines, read in full against its Stage 0 → A1–A7 → Gate A → B1–B6 → Gate B → 1.0 sequence. This handoff records how the current **Foundation 0.11.0** package maps to its requirements and which items still need a **real evidence-backed acceptance decision**. The earlier *Remaining Development Roadmap* does NOT override its priority order.

## Controlling boundaries

1. Native Kingdom Inc behavior is authoritative. `FlowElement/FlowIO/FlowPipe`, machines, items, research, combat, saves, UI, input and economy remain game-owned; Foundation may expose thin typed bridges, never a second gameplay simulation.
2. Foundation-created systems (registries, events, signals, config UI, tooling, mod persistence) are Stage B systems; mod-created things are not native merely because they wrap native functionality.
3. Stage B1–B6 may proceed because Gate A was accepted for development at the user's instruction. This does not turn prior *reported* runtime outcomes into archived, reproducible evidence.
4. All five B6 candidates are **REWRITE** by explicit user override, with functionality and mod migration preserved and documented. No automatic deletions.

## Roadmap execution and status reconciliation

| Roadmap section | Source inventory/evidence in this package | What is / is not independently established |
|---|---|---|
| **Stage 0 inventory & baseline** | Gate A native source freeze; current 396→407 production C# diff; 24-system file map | Third-party mod consumer inventory, clean installed 0.11.0 DLL hash and exact live baseline not archived here |
| **A1 machines** | 61 native class source matrix, A1 verifier, actual native class contracts | User previously reported all registered types inspected. Successful execution of every specialized machine action remains separate proof |
| **A2 FlowIO and throughput** | FlowIO/FlowPipe contracts, port/capacity/validation/route/transfer checks; native implementation unchanged | User reported all A2 tests passing. Machine-specific IO special cases remain dependent on real kingdom scenarios |
| **A3 items / crafting / economy** | Native game item/currency/resource/production bridge, contracts and A3–A7 verifier | Runtime test suite provisionally accepted by user; exhaustive save/transaction/economy operations not independently demonstrated here |
| **A4 research / adventures / combat** | Game API progression/adventure/combat contracts and source verification | Real quest/dungeon/war progression, item effects, combat and save-state transitions are fixture dependent |
| **A5 workspace / editing** | Native world and workspace operations, flow moving and A3–A7 verifier | Source-level native mapping; disposable save/placement/undo availability assertions need game execution and evidence |
| **A6 UI / input / camera / audio / assets** | Native UI factory plus game services and theme snapshot | UI screenshot checks previously accepted by user; current 0.11.0 F8/config and all UI scales not independently archived |
| **A7 native events / read-only data** | GameEventIntegration and source-specific native patches, event coverage | Real observed game event origins and complete transition matrices require live event reports |
| **Gate A** | `Docs/GameAPI/GateA` source hashes and contract matrices | Accepted to proceed by user; exact successful-operation and consumer evidence gaps were documented; do not backfill missing logs |
| **B1 R01/R02/R03/R04/R06/R07** | `Core` registries/lifecycle/dependency/events; independent B1 verifier | 0.10.7 one-shot user PASS; new 0.11.0 external consumer re-test not archived |
| **B2 R20/R21/R22** | Asset/content/save owner-scoped bridges and migration contract | Actual native content registration and schema save/reload roundtrips remain unproven by one-shot test |
| **B3 R08–R11** | Signal registry/search/property/link/adapters, preserved separate from pipes | Type routing smoke exists; provider removal/reinstatement persistence needs live fixture |
| **B4 R12/R13/R15** | Native-themed Mod Config and custom controls, machine panels, Stage B visible UI smoke | A one-shot success does not prove user clicks, keyboard focus, narrow layout, Options Apply/Cancel or screenshot visual approval |
| **B5 R16–R18** | Developer console/F8/inspector/diagnostics, full-suite config and log check | Real F8 opening needs game log and user-visible confirmation; no per-frame logging added |
| **B6 R05/R14/R19/R23/R24** | All five documented **REWRITE**, API bridge usage, no production C# deletions; user-visible behavior preserved by design | Cross-mod Harmony collision, missing tech roundtrip, custom workspace node save/load, UI clicks and compatibility fixtures not independently tested here |
| **Gate B** | 24-system file/verification traceability, exact native source diff, ABI risk and migration guide, Python static audit | Source gate is complete. Final 1.0 compatibility/runtime acceptance remains **conditional**, not independently certified |
| **Foundation 1.0 final work** | Current source package + existing SDK/docs/examples and migration guide | Not a 1.0 release: rebuild all examples against final DLL, prepare stable public API, versioned SDK/runtime/changelog, audit all missing features, and run installed reproducible tests first |

## Required next full-roadmap execution, not just checklist theater

The next audit should convert **every** original feature/"Partial"/"Missing"/"Implemented" entry in the controlling roadmap to an individual evidence-backed disposition: **native API provided, Foundation extension provided, intentionally absent from game, or gap**. For a missing feature that is not in Kingdom Inc, don't invent it to raise a completion score. For a promised Foundation feature (such as signal binding persistence or mod-authored UI behavior), don't classify a getter as complete without interaction/persistence evidence.

The most significant outstanding acceptance work falls into: (a) external mod ABI compatibility with the two B1 `IFoundationCore` member additions, (b) real native content/tech/workspace save/load with a temporarily absent provider, (c) native UI input and layout across resolutions, (d) two-mod Harmony conflicts and cleanup, and (e) full final packaging and build of the release SDK/examples. These are **not** silently marked PASS by the static source audit or the user's provisional acceptance.

## Source audit and verification artifacts

- `Docs/GateB/Gate-B-Compatibility-And-Decision-Record.md` — five B6 decisions, ABI break, immutable ID policy and release criteria.
- `Docs/GateB/Stage-B-24-System-Traceability.csv` — 24 system IDs mapped to files and runtime checks.
- `Docs/GateB/Gate-B-Source-Compatibility-Audit.json` — exact changed/unchanged/added production sources, SHA256 lock, and risk summary.
- `Docs/GateB/Gate-B-Static-Validation.json` — reproducible result, **source only**.
- `Docs/StageB/Mod-Migration-Guide.md` — actionable instructions for other mod authors.
- `Tests/GateB/GATE-B-AUDIT.cmd` — read-only offline audit (no game startup, saves or DLL changes).
- `RUN-ALL-TESTS.cmd` — sequential game-backed tests. Do not stack verifier DLLs; remove after use.

**Conclusion:** This is a Gate B source-analysis and compatibility-migration handoff. It prepares the requested next **full roadmap completion review** without claiming the whole 565-line document is already satisfied or releasing a falsely-certified 1.0 binary.
