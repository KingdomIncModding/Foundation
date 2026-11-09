# Core Stage B1 — Installed Runtime Verification

Do not run the older continuous A1, A2 and A3–A7 test DLLs simultaneously; they were previously linked to severe game FPS degradation. Stage B1 ships one **one-shot** test DLL only and does not need `-foundation-gameapi-mutate`.

1. Close Kingdom Inc. Extract the complete source ZIP under `KingdomInc\_Mods` (contains `Foundation\...`).
2. Run `Foundation\INSTALL-FOUNDATION.cmd`; installation must compile using actual game DLLs and must produce one production `Foundation.dll`.
3. Run `Foundation\Tests\StageB1\STAGE-B1-TEST.cmd` option **2**. If the verifier fails to build or install, stop; don't infer runtime success from Python checks.
4. Launch through Steam, allow a few seconds for Core initialization, exit cleanly. The verifier uses temporary module IDs, does not edit native game state, and writes one report to `Foundation\Verification\StageB1\stage-b1-verification.json`.
5. Use menu option **3**. A successful report matches the SHA256 and version of the currently installed Foundation and has **zero FAIL** entries.
6. Use menu option **4** to uninstall the independent verifier. Verify game performance without it.

Checks include the unified directory, dependency waits, activation order, required-provider deletion, transitive dependency cleanup, reload/restart, typed service discovery, version gating, shared legacy/Core events, subscriber exception isolation, cycle detection, and disposal. World transition ordering is checked statically in `Tests\verify_stage_b1.py` but **is not yet proven by the one-shot verifier's synthetic modules**. Test a real save load/unload before formally accepting the lifecycle rewrite.

This is not a test of native A1–A7 gameplay operations; their reported acceptance is the project owner's Gate A assumption.
