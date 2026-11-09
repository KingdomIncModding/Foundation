# Foundation UI 0.10.3 — Four reported visual defects, targeted correction

**Reference capture:** `UISmoke-20261109-090724.zip`, game at 1600×1000.
**Status:** source fixes completed; new binary compilation and Unity screenshots **not yet run** on this host.
**Baseline:** `Foundation_UI_0.10.3_090724_VisualReview_Complete_Source.zip`.

The previous result of 37/37 PASS was a **functional smoke status**, not visual acceptance. It was misleading to treat zero `VisualIssues` as evidence of no layout problems: the smoke harness only inspected the Foundation panel text layout and the Mod Settings Signal caption, and did not make visual warnings fail all cases. This update changes both gaps.

| Screenshot | Observed before fix | Source correction | New screenshot-smoke check |
| --- | --- | --- | --- |
| `132_settings-number_native_panel.png` | The numeric value `12` touches the pointed gold left cap of what appears to be an action button rather than a text-entry well. | Replace cloned Options button background with native-theme recessed well; 20px horizontal text gutter; native pixel font at 16, single-line clipped text. | Verify input field uses well backing, has a clipped text viewport and at least 16px clear gutters. |
| `133_settings-input_native_panel.png` | The string `Test text` likewise begins against the decorative button cap, with no proper input-field spacing. | Share the same fixed `CreateInputRow` control as Number; correct both cases at their common source rather than altering screenshots. | Same field geometry and inset checks, separately counted by the Test UI harness. |
| `136_settings-signal_native_panel.png` | A long technical source identifier wraps inside a one-row native dropdown and collides with its arrow/property row. | Keep full signal IDs internally, display short friendly names only. Duplicate names receive compact `(1)`, `(2)` suffixes, not mod GUIDs. Preserve NoWrap/Ellipsis. | Reject captions over 28 chars, raw internal IDs, multiline captions or captions intruding into arrow region. |
| `005_toggle_initial_panel.png` | The game's checkbox graphic is a tiny ~10px box with label crowded immediately beside it at the top-left, visually unlike the other control rows. | Resize the native checkbox *graphic* to 26px, checkmark to 20px, and place its label separately with a 56px left inset; keep the entire row clickable. Also enlarge the related native Mod Settings checkbox art. | Reject checkbox art narrower/shorter than 22px or label intersecting the checkbox. |

## New acceptance behavior

- Any recorded `VisualIssues` on a Foundation gallery control, machine panel, or native Mod Settings element now fails that case, rather than reporting PASS with warnings.
- Still capture the full game frame, panel and element, including after-action screenshots.
- Game-side visual inspection still matters; automated geometry tests cannot detect all optical alignment, wrong sprites, palette issues or Unity's effective scaling.
- **This does not mark UI 100%.** Compile the updated source against Kingdom Inc, re-install the independent screenshot-smoke verifier, capture again at 1600×1000 and one narrower resolution, and check four named PNGs again. After that, confirm actual keyboard typing, toggle clicking, dropdown selecting, Apply/Cancel, and world reload.

## Installation

1. Close the game and back up the currently working Foundation DLL.
2. Extract the **overlay ZIP** into `KingdomInc\_Mods` (contains `Foundation\...` paths). It changes the Foundation UI renderer and the screenshot-smoke verifier.
3. Run `Foundation\UI-FRAMEWORK-TEST.cmd` option **2** to build and install the Foundation DLL, then option **7** to compile/install the independent screenshot smoke plugin.
4. Launch game > load test kingdom > `UI Smoke + Screenshots` > use option **8** to package the new report.
5. Upload `UISmoke-*.zip` for the next actual screenshot visual pass.

This source revision does not introduce additional production DLLs and does not edit native Kingdom Inc assemblies.
