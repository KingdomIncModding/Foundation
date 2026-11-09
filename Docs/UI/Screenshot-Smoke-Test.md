# Foundation UI screenshot smoke test (0.10.3)

The original UI demo verification (`UI-FRAMEWORK-TEST.cmd` choice **4**) checks only the BepInEx log markers for demo initialization, one control action, and a machine popup. It **does not** perform a UI control sweep or take pictures.

This optional **UI Screenshot Smoke** consumer adds a one-click automated sweep inside the real Kingdom Inc Unity renderer, with PNG screenshots of each control and a machine/settings audit. It is **NOT part of the production Foundation.dll** and makes no changes to the underlying Foundation UI implementation.

## Install

1. Close Kingdom Inc.
2. Extract this source overlay to the `_Mods` directory, preserving the `Foundation/` prefix.
3. With your known-working **Foundation 0.10.3** installed, use `Foundation/UI-FRAMEWORK-TEST.cmd` and choose **7 — Install screenshot smoke** (or `Examples/UISmoke/INSTALL-UI-SMOKE.cmd`). This compiles `Foundation.UISmoke.dll` against your **installed** Foundation/game DLLs and installs the **test-only** plugin.
4. Launch Kingdom Inc in **normal graphics mode**, load a disposable kingdom, and click **UI Smoke + Screenshots** on the Mod Bar **once**. Avoid opening other menus or changing resolution while the test is capturing.
5. The plugin automatically opens individual test panels for 22 UI builder control variations, checks component presence, drives safe in-memory callbacks, captures before/after full-screen, panel-cropped and element-cropped PNGs, and visits the actual **Options > Mod Settings** menu to capture 10 distinct settings control elements. It also captures the game Settings/Installed Mods tabs. One screenshot is taken for each unique machine type **actually placed on the active flow**; it never activates a native machine action.
6. Let the sweep finish and check the BepInEx log for `[FOUNDATION UI SMOKE] FINISH`. You can continue playing after the Options menu is closed automatically.
7. Run **8 — Report + ZIP screenshots** from `UI-FRAMEWORK-TEST.cmd`, or `Examples/UISmoke/READ-UI-SMOKE.cmd`. This validates the generated PNGs, prints pass/fail/skip counts, and packages all screenshots and the JSON manifest into a single ZIP.
8. Upload that `UISmoke-YYYYMMDD-HHMMSS.zip` here to analyze actual visuals and failures.
9. After testing, close the game and run **9 — Remove screenshot smoke**. The production Foundation DLL remains unchanged.

## Output paths

```
KingdomInc/_Mods/Foundation/Verification/UISmoke/
    YYYYMMDD-HHMMSS/
        ui-smoke-results.json
        001_header_initial_screen.png
        001_header_initial_panel.png
        001_header_initial_element.png
        ...
    UISmoke-YYYYMMDD-HHMMSS.zip
```

The screenshots contain gameplay visible behind UI, so review the archive before sharing. To avoid a huge upload, you can send only the resulting ZIP; do not send old diagnostics or build outputs.

## Tests and their meaning

- **Control rendering:** header, text, button, toggle, slider, numeric input, text input, dropdown (collapsed and expanded), search, rectangular color swatches, signal source, signal property, tabs, columns, section, reordering, confirmation accept/cancel (including both modal states), typed property editor, live value, tooltip and scrolling.
- **Safe callback probes:** programmatically invoke Unity UI event callbacks for demo-only button, toggle, slider, number, input, dropdown, search, swatch, signal, tab, section, reorder, confirm, property, tooltip and scroll.
- **Mod Settings capture:** ten corresponding controls in `FoundationConfigMenu`, including dedicated source/property and color selector. Native Game Settings and Installed Mods pages are captured too. These screenshots are **visual only**; the tester never chooses Apply, never commits real configuration values, and restores/resumes the native menu.
- **Machine panels:** one per native runtime machine type **already on the current flow**. These are opened and screenshotted **read-only**, without changing native machine configuration or resources.
- **Results:** each case has `PASS`, `FAIL`, or `SKIP` in `ui-smoke-results.json`, along with filenames and any errors. PNG validity is checked by the report script. An interrupted run has no finished timestamp.

## Important limitations

**This is an actual in-game automated render and callback smoke test, not a proof of perfect UI.** A `PASS` verifies that the control was present, screenshot was captured, and its scripted callback behaved as expected. It **does not** verify that a mouse physically hit the right pixel, that text is unclipped, or that the theme looks right; those need visual review of the PNGs. It also cannot cover specialized machine panels for types not placed, saves/reloads, every possible mod compatibility scenario, or every input method. Existing Core/Machine regression suites must still run after framework changes.

The report viewer also generates a local **`index.html`** contact sheet with clickable full-screen images.

The screenshots require a **running visible game window**. Running the old isolated Foundation native smoke test with `-foundation-smoke` will **not** automatically produce UI screenshots, because it does not create this interactive gallery context.

If one control fails, the runner records it and moves to the next. It never attempts native machine mutations or overwrites player saves. The test plugin is separate from `Foundation.dll`; remove it once testing is complete.


## Compilation hotfix: `JsonUtility` CS0103 (2026-11-09)

Unity 6000 distributes `JsonUtility` in the optional `UnityEngine.JSONSerializeModule`
assembly. The test plugin previously referenced `JsonUtility` without referencing
that module, producing CS0103. The UI smoke test now serializes only its own
fixed-schema report as escaped UTF-8 JSON using `System.Text.StringBuilder`,
which needs no additional game DLL reference. The format remains compatible with
`Tests/Read-UISmokeReport.ps1`: fields are `StartedUtc`, `FinishedUtc`,
`FoundationVersion`, `GameResolution`, `ScreenshotFolder`, and `Cases`. Each
case includes `Name`, `Kind`, `Status`, `Rendered`, `Interacted`, `Note`,
`Error`, and `Screenshots`. No Foundation production code changed.
