# Gate B — Foundation 0.11.0 integration and compatibility audit

**Controlling design rule:** the original game's machine/IO/items/research/workspace implementations remain authoritative. Foundation-owned registries and UI are not replacements for game simulation. All five B6 systems are **REWRITE** by explicit user direction, never REMOVE; preserve their functionality and provide mod migration guidance.

**Baseline:** `Foundation-0.10.6-Gate-A-Native-API-Audit-Complete-Source.zip`.

**Source comparison:** 396 baseline C# files, 407 current C# files, 11 added, 23 modified, 0 deleted, 373 unchanged; all 396 baseline hashes match the shipped Gate A source-fingerprint manifest. **No intrinsic gameplay/machine/IO implementation changes**. Source checks executed using the original game source `KingdomIncSrc.zip`; they are not Unity runtime evidence.

## Start here

Read `Gate-B-Compatibility-And-Decision-Record.md` first. It documents all 24 systems, five mandatory B6 rewrite decisions, the **public `IFoundationCore` ABI risk**, migration rules and Gate B's conditional status. The executable inventory is `Stage-B-24-System-Traceability.csv`. `Roadmap-Completeness-Handoff.md` identifies remaining release evidence against the entire controlling 1.0 roadmap.

The regular `Tests/FullSuite/Run-FoundationFullSuite.ps1` now fixes the previous PowerShell `false` error and nested log-name bug, rejects stale verifier and visual reports, recovers old F8 configuration before starting a new run, and incorporates the Gate B static audit. Run `Tests/GateB/GATE-B-AUDIT.cmd` for safe source checks, or `RUN-ALL-TESTS.cmd` to compile the actual game mod and gather sequential runtime evidence. The runner never leaves more than one independent verification plugin installed per game session.

## Honest gate status

**Source: verified. Release: conditionally accepted for continued development, NOT independently 100%-certified.** User-accepted runtime tests allow continuing, but cannot replace external mod ABI, actual save roundtrip, F8 native UI, Harmony conflicts, and final installed DLL evidence. The Gate B audit does not change the 0.11.0 production Game API. There is no built `Foundation.dll` bundled in the source archive.
