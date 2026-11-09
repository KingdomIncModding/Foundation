# Foundation 0.10.2 - Unified Core + Machine source

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

This package is **NOT precompiled or runtime verified** in this Linux environment. The previous Core 320/320 and Machine 26/26 tests verified **the old two-DLL layout**, not this merged build.

1. Run the built-in Foundation native isolated smoke workflow when safe, keeping backups. Build+Install first; `-Verify` is an optional special mode that needs prerequisites (see `Tools/Development`).
2. To rebuild the independent machine verifier against the merged assembly, run `Tests\INSTALL-UNIFIED-MACHINE-TEST.cmd` (development only). This creates a second DLL temporarily **for testing**, not for players.
3. In-game: enter a disposable kingdom, connect machines, let resources transfer, add/change/remove a machine, return to menu, reload, return to menu, exit.
4. Run `Tests\READ-MACHINE-REPORT.ps1` for fresh 26/26 status. Delete the verification plugin DLL after successful testing. Rerun independent Core verification against the new DLL too.

## Compatibility notice

**Existing mods compiled with an AssemblyRef to `Foundation.Machines.dll` must be rebuilt to reference `Foundation.dll`**. Keeping the namespace/type names alone does not preserve CLR type identity across an assembly merge. Don't ship the old `Foundation.Machines.dll` alongside the merged build to make those mods work; doing so can cause duplicate plugins and conflicting types.

The regular package contains only Foundation.dll. Developer tools may compile independent test DLLs but are not distributed to players.

## Build status

- Source merging and static architectural checks: done.
- Compilation against your live installed game: **not run here**.
- Unity/BepInEx runtime regressions: **not run here**.
- Native mutation/save/reload hardening: needs separate evidence after merge.
