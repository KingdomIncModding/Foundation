# Foundation 0.10.6 — A1 Source Completion and Runtime Handoff

This is **one consolidated full Foundation source archive**, not an incremental patch. It follows `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md` controlling order: **Stage A Game API first, Stage B custom system rewrites only after Gate A**.

## Completed in source

- Exhaustive original-source declaration inventory: **61 concrete native FlowElement subclasses**, including leaf recipe/resource machines, storage subclasses, logistics, commerce, adventures, utility and native/internal classes. Two abstract bases and two slot helper classes are classified explicitly rather than counted as placeable machines.
- Native `FlowElement.GetAllTypes()` prefab discovery and per-type description; exact CLR type matching avoids tagging a modded subclass as vanilla.
- A1 per-machine native scalar/specialty reads integrated into **every existing `MachineSnapshot` path** without changing the old `AdditionalProperties` API.
- Native-safe machine configuration wrappers for appraiser/accountant comparisons, war selection, Collector.NextInput, the native Outfitter.Timeout setting (research-gated), portal and armory channels, plus existing native recipe/resource/manual craft/logistics/IO paths.
- Dynamic native capability list and real-game production/purchase/refund/crafting/resource cost readers, preserving native rules.
- Per-class original game file SHA256, public member source reference, inheritance family, property/operation matrix, SDK consumption guide.
- Independent, read-only runtime verification consumer (not part of the player runtime build), installation script, dynamic per-native-prefab PASS/FAIL/PENDING report.

## Validation boundaries

- Static tests check source contracts and exact original game source SHA256. They cannot compile Unity/BepInEx assemblies in this environment.
- The new C# source must compile and run against the **real installed Kingdom Inc**. Do not call Stage A1 runtime verified before this succeeds.
- A1's dynamic verifier only queues `A1.Type.<Type>` checks for **actual native registered prefab types**, not every source class (some are dev-only). Each check passes when the real type is observed and its native state and IO snapshot agree. PENDING means the machine was not encountered; it does **not** mean failure.
- Source declarations present in code may not match buildable content if a game update changes `FlowPrefabSet.Ordered`; this should be handled as a version audit, not simulated registration.
- Runtime mutating operations should be tested separately in a **disposable kingdom**, with game state and save/load assertions. The included A1 test consumer performs no changes to live game objects.
- A2 is user-reported runtime green on Foundation 0.10.4. A3–A7 were source-complete but still runtime-pending as of this baseline. Rerun all against the unified 0.10.6 binary. Source success doesn't inherit runtime success across versions.
- The overall Gate A remains **open** until A1–A7 acceptance and platform/build/source compatibility verification are recorded.

## Commands on Windows

1. Close Kingdom Inc, back up the installed Foundation binary, extract the entire `Foundation/` folder into `KingdomInc\_Mods`.
2. Run `Foundation\Tests\NativeMachines\INSTALL-A1-TEST.cmd`. It uses the existing unified Foundation installer to compile against the installed game and then builds only the **development verification** consumer `Foundation.NativeMachinesVerification.dll`.
3. Launch the game, load a **disposable** kingdom, place multiple different types of native machine and let their native state/IO update. Use native UI to change their supported settings normally.
4. Quit, run `Foundation\Tests\NativeMachines\MACHINE-A1-TEST.cmd`, select **3 — Read Report**. Inspect every `A1.Type` PENDING and any FAIL; verification report is tied to version and SHA256 of the installed Foundation DLL.
5. Verify A2 and A3–A7 runtime tests, machine/core UI regression tests with the same freshly compiled Foundation DLL. Remove the independent verification plugins when finished.

**No background verification has been performed and no new DLL has been built in this package.**
