# Foundation UI Screenshot Audit — 2026-11-09 05:58 local

Source evidence: `UISmoke-20261109-055822.zip` uploaded after Foundation UI 0.10.3 was built and launched on Windows / Kingdom Inc. The archive was CRC-checked and contains `ui-smoke-results.json`, an HTML gallery and **158 PNGs**.

## Recorded outcome

| Area | Pass | Fail | Not fully exercised |
|---|---:|---:|---|
| 22 panel-control cases | 21 | 1 | Some pass only from scripted callbacks |
| Native machine panel screenshots | 3 | 0 | Only Forge, Mine and Shop were placed/observed |
| Native and mod settings | 12 | 0 | Screenshots only; no Apply/Cancel state verification |
| **Total** | **36** | **1** | **Full UI remains open** |

Game resolution: **1600 × 1000**. The script recorded build **Foundation 0.10.3.0**.

The lone reported failure was **Button Tooltip** (`Tooltip status text rendered=False`). Source review of `Examples/UISmoke/FoundationUISmokePlugin.cs` confirmed a false-negative probe: the test selected the first `IPointerEnterHandler` on the button; Unity's `Button` itself implements that interface, so the probe did not necessarily call Foundation's attached `FoundationPanelTooltip` handler. This is a **test harness defect**; actual pointer-hover function is not established as passed or failed by this run.

## Visual findings in the captured PNGs

1. **Transparent control and machine panels (high priority).** Screenshots such as `001_header_initial_screen.png`, `014_dropdown_expanded_panel.png` and `044_machine-01-Forge_native_screen.png` show game pipes, resources and the flow grid visible immediately behind the panel's controls. The sampled native background graphic does not provide a reliably opaque backing. The user cannot read long text and open dropdowns consistently against busy flows. In contrast, `127_settings-text_native_panel.png` and the other Options screenshots show the game's opaque dark-purple/gold Settings theme.
2. **Machine panel reuses prior scroll position (high priority).** `043_scroll_after-action_panel.png` records a scroll-to-bottom immediately before native machine screenshots. `044_machine-01-Forge_native_panel.png` starts with its `Crafting Recipe` header cut at the top. The machine panel uses the same scroll host as the gallery and was not returning to the top upon opening a new definition.
3. **Signal selector label overflow (medium priority).** `022_signal-property_initial_panel.png` and `023_signal-property_after-action_panel.png` show the long `urakka.foundation.example.ui-smoke.test-source` source ID wrapping onto multiple lines inside a single dropdown row, intruding on the exposed-property selector. Native-friendly human-readable source names should appear in the dropdown, while internal IDs remain the selected values.
4. **Verified positive controls.** The `Dropdown` captures include an expanded list; `Search` after-action filters the list down to one result; `Toggle`, `Slider`, `Number Input`, `Text Input`, `Color Swatches`, `Tabs`, `Section`, `Reorder`, `Confirmation Accept` and `Confirmation Cancel` all report scripted callback success. These tests do not replace real pointer/keyboard or game-state persistence tests.
5. **Coverage limitation.** Only three distinct machine types were placed in the flow. The harness cannot claim rendering proof for the other Foundation native machine definitions on the basis of this run. `Options Game Settings` and 11 other Options captures are read-only and cannot prove saving/validation.

## Fixes in this UI audit source revision

- `FoundationPanelHost`: reliable 98%-opaque dark plum backing and thin gold frame matching the visible game's Options palette; no reliance on a transparent sampled sprite.
- `FoundationPanelHost`: reset shared `ScrollRect` to the top whenever a different panel is opened, fixing the prior gallery-to-Forge scroll carryover.
- `FoundationPanelBuilder.Signal`: display readable source/property names instead of raw long IDs, retaining full IDs internally; distinguish duplicate source display names with the owner ID.
- `FoundationUISmokePlugin`: invoke the attached `FoundationPanelTooltip` handler specifically when simulating pointer entry, instead of accidentally invoking the Unity button's native hover handler.
- Add source-level regression assertions for these cases.

## Verification and user actions

These fixes have **source-level tests only** in this container: UI static checks **39 PASS, 0 FAIL, 1 SKIP (optional native signature check)**; screenshot harness source checks PASS; smoke isolation source checks PASS. **This UI audit revision has not been compiled or tested in Kingdom Inc on Windows.** The earlier 36/37 runtime audit refers only to the original screenshoted UI, not to these fixes.

1. Keep the last working `Foundation.dll` as a rollback and close Kingdom Inc.
2. Extract the *UI Audit Fix Overlay* into `KingdomInc\_Mods\`, overwriting the `Foundation/` source files.
3. From `Foundation\UI-FRAMEWORK-TEST.cmd`, use **2** (build/install Foundation), then **7** (rebuild/install screenshot smoke verifier against this newly installed DLL).
4. Load a disposable test kingdom and click **UI Smoke + Screenshots**. Use **8** to generate the next report ZIP. Confirm tooltip PASS, opaque windows, readable signal source and complete Forge header.
5. Test actual mouse hover, dragging, selection, typing, escape/close, settings apply/cancel and multiple world loads. To audit all 18 default machine panels, provide a test flow containing each distinct native machine type and rerun the screenshot smoke. Repeat Core and Machine regressions against the rebuilt Foundation DLL.

Treat this as a visual QA remediation pass, **not a 100% verified UI release** until the new build and in-game retest succeed.
