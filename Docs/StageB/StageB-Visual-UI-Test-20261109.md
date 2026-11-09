# Stage B — on-screen controls missing: corrected verifier

## Root cause (verified in supplied source)

`Tests/StageB/StageBVerificationPlugin.cs` registered a **blank** panel with `Build = _ => {}` for `B4.OwnerPanelAndSettings` and immediately disposed its registration scope. Likewise `B6.ModBarCompatibility` registered **only `ModBarText`** and disposed it inside the same one-shot check. It had no click-enabled test controls and never opened a panel. Its two visual checks correctly reported `PENDING` but the wording `PASS` on the registry-only checks was misleading.

The native `ModBarButton`, `ModBarToggle`, `ModBarDropdown` and Foundation panel APIs were *not demonstrated to be broken* by this report. The verifier simply never rendered an interactive test. This correction changes **the independent test plugin only**, not the production Foundation UI or the game's native UI.

## Corrected test design

- The same **one** temporary `Foundation.StageBVerification.dll` now registers a persistent `STAGE B UI TEST` button in `FoundationApi.ModBar`, with a visible native checkbox, dropdown and separator beside it. Unlike the old registry smoke, these registrations live **until the verifier plugin is unloaded**.
- In a loaded kingdom, the Mod Bar lives **directly beneath the currency HUD**. The center chevron collapses/expands it. Click `STAGE B UI TEST` to open a themed `FoundationApi.Panels` popup (not a custom canvas).
- The popup has a real Foundation Button, Toggle, Dropdown, Text Input and a manual visual-approval button. The controls invoke callbacks on the Unity main thread. They only modify temporary test-plugin variables, not native machines, research, saves or pipe state.
- The original automated suite still runs once and writes its original SHA-verified `stage-b-verification.json`. A **separate** `stage-b-visual-verification.json` records real user actions, but **only when the visual plugin is initialized or a UI callback fires**. No `Update` loop, continuous scanning, or periodic logging.
- The report reader (menu option 3) reconciles visual evidence for `B4.RenderingVisualAudit` and `B6.ModBarNativeVisual` only when the visual report matches the installed Foundation version/SHA-256 and was produced after the current test DLL installation. It does **not** silently convert other pending checks to PASS.
- On unloading, all scoped test controls are disposed; production Foundation remains installed.

## How to use

1. Close Kingdom Inc. Confirm old separate screenshot/verifier DLLs are not installed. Keep `Foundation.dll` installed.
2. Re-extract the **complete source archive** into the existing `_Mods` folder and run `Foundation\Tests\StageB\STAGE-B-TEST.cmd`, option **2** to **rebuild and reinstall the verifier**. You do not need to change Foundation production APIs or use a mutation flag.
3. Launch Kingdom Inc and load a kingdom. The menu/title screen does **not** have the game HUD.
4. Find `STAGE B UI TEST` immediately beneath the top currency HUD. If only a small chevron is visible at top center, click it to expand the Mod Bar. If the HUD is hidden, show it using the game's normal HUD control. If there are numerous other mod-bar entries, use its left/right scroll arrows.
5. Click **STAGE B UI TEST**. Verify the full panel's controls are visible and themed. Click **CLICK ME - TEST BUTTON**, change **TEST CHECKBOX**, select **First** or **Second** in **TEST DROPDOWN**, and replace the **TEST TEXT FIELD** value, then **leave the input** to commit it.
6. On the HUD itself (not only in the popup), change **Test Toggle** and **Choose option**. This checks actual HUD callbacks as well.
7. Finally click **I CHECKED THE VISUAL LAYOUT** in the panel, after you've inspected the controls. Only then can the visual checks be marked PASS. The visual report is direct user acceptance evidence; it is **not** an automated screenshot-pixel inspection.
8. Exit the game and select option **3**. The report now includes four detailed visual checks and reconciles the two original visual pending statuses where supported. If any check remains pending, the reason lists the exact missing interaction.
9. With Kingdom Inc closed, select option **4** to remove the temporary verifier and its visible UI. Do not remove production `Foundation.dll`.

## Remaining unrelated Stage B acceptance work

The initial report contained **20 PASS, 0 FAIL, 7 PENDING**. This correction targets the two **B4/B6 on-screen visual** pending cases. The following **five** still require separate safe, real-world evidence and should remain pending until exercised:

- `B2.NativeRegistrationSaveRoundTrip`
- `B5.ConsoleKeyboardF8`
- `B6.MissingTechRoundTrip`
- `B6.WorkspaceNodeSaveLoad`
- `B6.NativePatchConflictCleanup`

A success on visual controls does not validate saved mod content, keyboard input, missing-content recovery, workspace-node restoration or patch conflicts. Stage B is not automatically 100% accepted from this UI fix.

## Regressions/compatibility

No public Foundation interface, native Game API method, stable mod content ID, save schema or production DLL composition changed. `FoundationApi.ModBar` and `FoundationApi.Panels` remain the single native-themed UI path, preserving existing dependent mod code. The additional visual probe is **test-only** and can be uninstalled independently. If a UI control fails to appear, check the Stage B plugin's `[STAGE B VISUAL]` BepInEx messages and the `warning` in `stage-b-visual-verification.json` instead of treating registry PASS as proof of a rendered button.

The production DLL is unchanged by this verification-only fix.
