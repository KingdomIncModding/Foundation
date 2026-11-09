# Foundation 0.9.1 — Native-save and callback hardening

**Status: Source candidate; not a built or gameplay-verified Windows release.**

Foundation 0.9.1 is an incremental revision of the original 0.7.1 framework and 0.9.0 build kit. It retains the same BepInEx plugin ID and the established research, workspace, flow, Mod Bar, and settings APIs. No game source or third-party managed DLLs are included in the source package.

## Changes implemented in actual C# source

1. **Missing research recovery:** `GamePlayer.FromJson` silently skips saved technology IDs when their defining mod isn't installed. `MissingResearchRecovery` captures such IDs before `SaveGame.LoadState`, and merges them into the native `Player.Tech` array when saving. A new kingdom clears the retained set. It does not simulate/purchase missing research or override the native tree.
2. **Failed-migration protection:** A mod save provider whose `Load` callback throws cannot overwrite the previously retained save payload on subsequent `SaveCurrentState` calls. This helps protect worlds after a bad mod upgrade.
3. **Future save-format retention:** Unknown malformed/forward-version entries in `FoundationModData` are retained verbatim when possible rather than silently dropped. A late provider registration cannot replace an uninterpretable entry.
4. **Game-hook exception isolation:** Workspace element observation and native pipe-event callbacks are protected from exception propagation. Faulty event observers should not interrupt the original game operation.
5. **Native save fallback:** If Foundation's save extension fails, the hook removes the partially added mod-data root, restores the native checksum algorithm, and reports the error. **This fallback does not preserve a newly failed callback's unpublished state; report the error and restore from a backup.**
6. **Critical smoke-test isolation fix:** An isolated executable still uses the same Unity `persistentDataPath` as the real installation. The previous smoke-test guard blocked save loading but would allow `GameManager.OnApplicationQuit` to overwrite the user's `autosave-exit.save`. Diagnostic mode now redirects the native save directory into its test runtime **and patches `SaveGame.Store` to block all native disk saves**. This is installed only with `-foundation-smoke`; normal gameplay saving is unchanged.
7. **Diagnostics and regression coverage:** The developer inspector reports retained unknown research count. Source runtime checks now exercise unknown mod-data retention, load failures, missing research round-trips, and native `Secret` recomputation.

## Verified offline with supplied artifacts

- Compared targeted Harmony patch method signatures to the decompiled Kingdom Inc source.
- Checked the supplied `KingdomInc_Data.zip` for the eleven managed assemblies referenced by the framework's game integration.
- Ran structural C# source scanning and script-level checks for both read and write isolation of Unity's shared save path.

**Not verified:** C# compilation, BepInEx plugin startup, Unity UI behavior, performance, or gameplay save/load. A complete release requires `bootstrap-windows.ps1 -GameRoot <game-dir> -Verify -Install` on Windows with BepInEx and Kingdom Inc installed. The command deliberately blocks installation without successful isolated verification.

## Remaining risks / non-goals

- **Unavailable mod content:** An unknown workspace element is inert; content-specific upgrades and other dependencies may need their original mod to load correctly. Back up saves before removing mods.
- **Schema callbacks:** Version numbers are communicated to mod providers; they must implement their own migrations. The framework preserves raw data when load fails but cannot synthesize a mod-specific migration.
- **Performance:** Signal getters run periodically on Unity's main thread. Expensive getters remain the mod author's responsibility.
- **Multiplayer:** No claim of multiplayer authority or synchronization is made; native gameplay determines any network semantics.
- **Runtime testing:** Only the actual game can verify Harmony patch ordering and Unity lifecycle behavior.

## Build paths

To compile with game data staged from the uploaded archive:

```powershell
.\bootstrap-windows.ps1 -GameDataZip '..\KingdomInc_Data.zip'
```

To build, run isolated game tests, package, then install:

```powershell
.\bootstrap-windows.ps1 -GameRoot 'C:\Path\To\Kingdom Inc' -Verify -Install
```

The latter assumes BepInEx 5 is installed. Add `-InstallBepInEx` only when the game does not already have a compatible BepInEx installation. Keep a backup of your game saves and your previous Foundation DLL.
