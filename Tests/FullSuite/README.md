# Foundation — Full-Suite Runtime Test Runner

**Launcher:** `Foundation\RUN-ALL-TESTS.cmd`  
**Driver:** `Foundation\Tests\FullSuite\Run-FoundationFullSuite.ps1`

This is the **single workflow** for collecting runtime evidence from A1–A7 and B1–B6. It does not create a new production DLL, replace Foundation's existing verification implementations, or claim success for unexercised gameplay. The previous per-stage `.cmd` files remain supported for compatibility, but operators no longer have to open each of them to perform the coordinated suite.

## Critical: Why F8 did nothing

`src/Foundation/Bootstrap/FoundationPlugin.FoundationDeveloperConsole.cs` binds BepInEx `[Developer] EnableConsole` with **default `false`**. When disabled, pressing F8 does nothing by design. The previous Stage B verifier neither enabled this option nor checked whether F8 produced an actual visible console. The full-suite runner now enables the option **temporarily**, then restores the exact previous BepInEx config file. If no config existed, the runner removes its temporary config file afterward. A one-line event is logged only when F8 actually toggles the inspector; no per-frame diagnostic printing is added. A human still confirms the inspector and tabs appeared.

If using the ordinary Foundation installation *without this runner*, opt in to the developer console by editing:

```ini
# BepInEx/config/urakka.kingdominc.foundation.cfg
[Developer]
EnableConsole = true
```

Restart the game, load a kingdom, and press F8. Keep the option false for ordinary play if you don't want the inspector enabled. This remains an optional developer feature, not a gameplay dependency.

## Steps

1. Close Kingdom Inc. Back up any important kingdoms before tests that explicitly modify saves. Confirm completed verification plugins have been removed.
2. Extract the **complete** Foundation ZIP into `KingdomInc\_Mods`. Run `Foundation\RUN-ALL-TESTS.cmd`.
3. Choose **1 — ALL tests**, or **2 — Stage B only** if A1–A7 already passed. The runner builds the production Foundation once against the actual installed game, then performs source checks.
4. For each selected stage, it **builds only that stage's independent test DLL**, launches the game through Steam if its app ID can be verified, or asks you to launch Steam manually. The script waits for the game process to exit.
5. In the **B1–B6** session, expand the HUD Mod Bar, click **STAGE B UI TEST**, interact with the HUD toggle/dropdown and all panel controls, and click **I CHECKED THE VISUAL LAYOUT**. Press **F8** to open the Foundation Developer Inspector and inspect all tabs. Close the game, then answer the runner's F8 visual-confirmation question.
6. After each game session, it collects native reports, verifies version/hash, and **removes that stage's temporary DLL** before proceeding to the next. The Stage B developer-console setting is restored after the Stage B game exits.
7. Choose **3 — Read latest report** at any time. Results are stored at `Foundation\Verification\FullSuite\run-<timestamp>\summary.json` and `summary.md`, with per-stage JSON and logs. The shortcut exit code is **0** only for all PASS, **1** if any FAIL, **2** if PENDING and no FAIL. This is intentional.

## Safety

- Running all stages requires **several sequential game sessions**, not multiple verification DLLs running at the same time. This avoids the previous FPS degradation.
- The runner does **not** set `-foundation-gameapi-mutate` or `-foundation-nativeio-mutate`. It does not create content, patch external mods to induce conflicts, rewrite saved technologies, or pretend a save/reload scenario happened.
- The source-level checks and one-shot smoke cases can finish automatically; genuine Unity input, visuals, save/load and external-mod compatibility **require gameplay/user interaction**. Any uncovered acceptance case remains **PENDING**, never a fabricated PASS.
- The runner will not delete unknown verification DLLs. It refuses to start if other verifiers remain installed. It removes only its own named test assemblies and never deletes `Foundation.dll`.
- It creates an on-disk backup of the original console config in `BepInEx\config` during the test. In normal operation this file is removed after restoration. If Windows/PowerShell crashes, close the game and use **option 4 — Cleanup** to restore the config from the recovery marker.
- Do **not** share raw `BepInEx\config` or backups publicly; config files may contain private API keys. The combined report records only test IDs, hashes, and outcomes, not configuration contents.

## Direct PowerShell usage

From the Foundation root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\FullSuite\Run-FoundationFullSuite.ps1 -Action Run -Scope All -Launch Auto
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\FullSuite\Run-FoundationFullSuite.ps1 -Action Run -Scope StageB -Launch Manual
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\FullSuite\Run-FoundationFullSuite.ps1 -Action Report
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\FullSuite\Run-FoundationFullSuite.ps1 -Action Cleanup -Scope All
```

`-Launch Auto` prefers the verified Steam app ID and otherwise prompts for a normal Steam launch. `-Launch Manual` always lets you launch via Steam. `-SkipFoundationBuild` uses an existing installed DLL (not suitable when testing newly modified source). `-SkipStaticChecks` records skipped checks as PENDING, not PASS.

## What the runner does *not* assert

The existing Stage B runtime verifier intentionally leaves **B2.NativeRegistrationSaveRoundTrip**, **B6.MissingTechRoundTrip**, **B6.WorkspaceNodeSaveLoad**, and **B6.NativePatchConflictCleanup** pending until real fixtures exist. F8 and the two interactive UI checks can be promoted to PASS only with actual observed input/visual evidence. Native A1, A2 and A3–A7 dynamic scenarios similarly require an appropriate test kingdom. These are acceptance requirements, not reasons to invent a fake simulation or to mutate the user's main save.

## Build status of this deliverable

Static source checks were executed in the source-authoring environment. **The full Windows BepInEx/.NET build, actual Unity runtime sessions, real F8 input and UI visuals have not been executed in that environment.** Use the runner above for the definitive evidence.
