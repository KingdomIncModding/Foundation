# Foundation UI — visual baseline and theme acceptance audit

Reference: user-provided `UISmoke-20261109-055822.zip`, 1600×1000, 158 PNGs, 37 test cases (36 PASS, 1 tooltip harness fault). This review is for the **before-theme screenshots**. Source code was patched in the subsequent theme pass, but **no post-theme screenshot exists yet**.

## Observed before-theme issues and remediation

| Actual captured element | Visual issue relative to native Options | Theme remediation in source | Post-patch visual status |
| --- | --- | --- | --- |
| Popup base, all panels | Game flow drawn behind text, black/translucent appearance | Opaque native-plum backing, game `DecoRectBorder` nine-slice | **RETAKE REQUIRED** |
| Header / Text | Oversized plain floating text, no decorative chrome | Native pixel font, 13–21 sizing, length-aware text row height, native border | **RETAKE REQUIRED** |
| Button / confirmation / paging | Flat fluorescent-green button bar | Game Options button sprite and native hover/pressed ColorBlock | **RETAKE REQUIRED** |
| Toggle | Bare checkbox and label, weak structure | Keep native checkbox, unify label font/cream text | **RETAKE REQUIRED** |
| Slider | Flat green track and generic handle | Dark well, brass fill, native Options thumb/`ScrollHandle` art | **RETAKE REQUIRED** |
| Text / numeric fields | Bright-green input background fights game's Options palette | Dark plum well with native border and warm text | **RETAKE REQUIRED** |
| Dropdown / search results | Native dropdown exists, results look like unrelated flat green bars | Dark framed dropdown field, native-game paged result buttons | **RETAKE REQUIRED** |
| Signal source/property | Internal IDs wrap and interfere with second dropdown | Previously replaced with readable source/property names; now dark framed dropdown style | **RETAKE REQUIRED** |
| Color swatches | Unframed primary-color rectangles | Preserve each swatch hue, add thin brass outline, selected brass frame | **RETAKE REQUIRED** |
| Tabs / collapsible sections | Bright-green full-width controls | Native Options button art and reduced title font | **RETAKE REQUIRED** |
| Reorderable items | Flat green rows over flow canvas | Framed dark rows; no background transparency | **RETAKE REQUIRED** |
| Dynamic properties / telemetry | Undecorated floating labels | Native panel surface + readable font; unchanged native values | **RETAKE REQUIRED** |
| Tooltip | Automated test chose Unity Button hover handler (not Foundation tooltip) | Prior targeted-probe correction; new layout smoke flags nearby text | **RETAKE REQUIRED** |
| Scroll overflow | Reopened panels inherit scrolled position | Previous reset-to-top fix retained, canvas-aware panel fit | **RETAKE REQUIRED** |
| Forge / Mine / Shop panels | Heading clipped, hard-to-read machine GUID, green tabs/buttons | Remove duplicate subheadings and internal GUID from player UI, theme actions/tabs | **RETAKE REQUIRED** |
| Game Settings and Mod Settings | Native plum/gold theme already cohesive | Preserve `ModConfigTemplates` clones exactly | **KEEP, REGRESSION CHECK** |

## Baseline native style measurements

Native Options raster panel: RGB `(38,26,35)` (`#261A23`). Dark input well target: `#1A121A`. Warm white/ivory text `#EADFCB`; secondary `#CDBEA1`; brass accent `#AE8249`. All palette pairings verified in `Tests/verify_game_theme.py` at contrast >= 4.5:1, with normal text >= 7:1.

Source assets were inspected: game sprite `DecoRectBorder` has a 32×32 texture, 8px border on four sides; `Button02` and `Button03` are 96×26 with 15px/12px nine-slice borders. `DefaultBorderDark` is 32×32 with 8px border. These are actual names and dimensions in the uploaded asset snapshot, not speculative resources. `Docs/UI/Game-Asset-Theme-Snapshot.json` stores paths and SHA256 fingerprint for detecting game updates.

## Mandatory new screenshot pass

After building this theme revision and reinstalling the development-only screenshot verifier, run **UI Smoke + Screenshots** again. The verifier now includes `VisualIssues[]` warnings for geometric text overlap, out-of-bounds text, unexpectedly large fonts, and low-contrast labels. Any warning must be reviewed; an empty warning list is not sufficient on its own. Compare every new screenshot against native Options, at original resolution and thumbnail scale. Test at 1600x1000 and at least one smaller game window. Check real mouse hover and dropdown expansion; scripting callbacks alone cannot establish usability. Capture all 18 specialized default panels across a sufficiently populated test flow, not just the original 3 types.

**Completion condition:** no jarring foreign-looking controls, no text collisions, no low-contrast labels, no cropped titles/rows, no visual drift from the native Options interface; passing independent callback/runtime test reports and a reviewed post-change screenshot ZIP. This condition is NOT yet met because the new game-side render has not been captured.
