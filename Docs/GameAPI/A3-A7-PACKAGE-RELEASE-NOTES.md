# Foundation 0.10.5 — A3–A7 consolidated native Game API source package

**Release type:** Complete Foundation source, NOT an incremental/overlay package.  
**Date:** 2026-11-09.  
**Baseline:** Foundation 0.10.4 A2 complete source, based on the user's 2026-11-09 09:41 source export.  
**Game reference:** `KingdomIncSrc.zip` (source-only; final installed-game assembly parity pending).  
**Controlling specification:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`.

## Implemented source work

- **A3** — Native item and enhancement reads, equipment attributes, item value/unlock rules; native recipe requirements, costs and results; native wallet affordability, resource cost multipliers (preserving `double` precision), farm plot prices and forest worker utilization and eligibility.
- **A4** — Native research dependencies, locking, costs and tiers; native quest rewards/duration, dungeon availability/stages/costs and war stages; native combat damage, armor, traits and equipment-compatibility reads. Existing game mutations remain forwarded to game methods.
- **A5** — Native workspace contents and counts; real `WorldFlowManager.MoveToFlow` to move disconnected machines or decorations between existing flows without introducing independent graph state or moving connected pipes.
- **A6** — Native `GameUI`, `PlayerInput`, `WorldFlowCamera`, loaded locale catalog, native translation; retained native audio, save and sprite APIs without custom UI rewrite.
- **A7** — New game-origin, post-commit observations for workspace switches, popup visibility, human equipment changes (including conflicts), and combatant health/energy changes; prior native economy, research, quest, dungeon, war and machine hooks preserved.

## Architectural decisions

- **Single production DLL:** `Foundation.dll` only.
- **Development test DLL:** `Tests/NativeGameApi` builds a separate, *temporary* verification DLL, not a player package component.
- **Stage B remains frozen:** no rewrite of signals, UI registries, Foundation event bus, custom content registries, or persistence.
- No simulated game transport, speculative throughput caps, fictional batch/undo/clone APIs, or custom second authoritative game state.
- C# public API changes are additive, except refining the new `ResourceCostMultiplier` API to match the game's native `double` before any binary release.

## Verification and acceptance

Source-level verification executed against the extracted game source: **137 A3–A7 static contract checks passed**, **35 A2 static checks passed**; previously established UI source/theme/smoke isolation checks also pass. This confirms source-level expectations, not a C# compilation, a live Unity test, all machine type coverage, or final Gate A completion.

The **installed Windows C# build and A3–A7 in-game checks remain pending**; this container has no installed Kingdom Inc binaries or .NET C# toolchain. Use `Tests/NativeGameApi/INSTALL-A3-A7-TEST.cmd` to build/install the entire source with the real game references and the independently compiled verifier. Run `Tests/NativeGameApi/GAME-API-TEST.cmd`, option **3**, after exercising the native game scenarios. The report must show **FAIL=0** and all required tests **PASS** to certify A3–A7 runtime acceptance. A1 and Gate A require separate native coverage and installed verification.

See `A3-A7-Native-Game-API.md` for original game class evidence and detailed testing instructions, and `A3-A7-Stage-Status-Matrix.md` for remaining coverage/acceptance conditions.
