# Foundation Game-Authentic UI Theme Contract

**Status:** Foundation 0.10.3 source theme pass, pending Windows compilation and **new in-game visual screenshot verification**.

## Source of truth (immutable until a game update)

The user's `Assets.zip` export is the canonical reference for the currently installed Kingdom Inc build. Its SHA256 fingerprint, individual native sprite names, nine-slice borders, and origin archive paths are recorded in `Game-Asset-Theme-Snapshot.json`. Do **not** replace the references with generic Unity skins, externally sourced icons, stock imagery, or made-up textures. When the game updates, update the asset snapshot and rerun screenshot visual QA before changing the theme.

This snapshot contains Unity serialized `.json` and `.bin` data, **not directly ready-to-import PNGs**. Foundation therefore accesses the *game's own loaded Unity sprites* at runtime (by the names documented in the snapshot), preserving pixel art and the game's correct nine-slicing without shipping third-party textures in a mod package. When an exact sprite isn't loaded, use the measured native color fallback and record the limitation in the visual QA. The vanilla **Options UI** takes precedence for button textures. Existing Options/Mod Settings controls already clone native game components and should not be reskinned.

## Visual basis — inspected screenshots

The 2026-11-09 UI Smoke screenshot archive provided 158 images and 37 panel/settings/machine cases. The native Options screen uses deep eggplant/plum backgrounds, gold/copper beveled trim and buttons, compact warm-white pixel text, and dark value wells. The existing custom Foundation panel UI was visibly different: bright green button bands, unframed controls, and initially transparent black windows. These are unacceptable for a public UI API. Previous visual audit fixed transparency, scroll carryover, and wrapped signal IDs; this pass changes the *remaining custom controls* to use native asset names and the measured native palette.

## Asset names and control mapping

| Component | Game source | Foundation approach |
| --- | --- | --- |
| Panel background | Native Options plum surface | Fully opaque RGB (38,26,35), under game sprite art |
| Panel border | `DecoRectBorder` / `DefaultBorderDark`, each 32x32 with 8px nine-slice border | Load native Sprite by exact name, use `Image.Type.Sliced` |
| Action, tabs, close, paging | Options `Apply` / `Cancel` template; else `Button02` / `Button03`, 96x26, 15x12 nine-sliced | Preserve native button sprites and hover/pressed/disabled `ColorBlock`, never flat green bars |
| Input fields, slider wells, dropdown surfaces, draggable rows | `DefaultBorderDark` / `SimpleBox` / `SelectBox` | Opaque dark wells plus game art border, never a fluorescent button background |
| Checkbox | Game native `Toggle` | Preserve checkbox icon and behavior, style native label in warm-white |
| Fonts and hierarchy | Actual native TMP font asset | Reuse font, reduce gallery font to max 18, header max 21, min 13 |
| Progress/slider fill | Native brass/gold accent | Single measured accent; dark well behind it |
| Swatch color | Actual selectable user color | Preserve hue exactly; add thin brass outline without opaque overlays |
| Close/hover/tooltip | Native button styling and UISounds | No out-of-theme icons and no hidden-only essential label |
| Settings controls | Existing `ModConfigTemplates` clones from `OptionsUI` | Leave native Options control visuals intact |

## Contrast and typography rules

Measured fallback colors (from 1600x1000 screenshots): panel `#261A23`, inner well `#1A121A`, primary text `#EADFCB`, muted text `#CDBEA1`, brass `#AE8249`.

**Contrast ratios:** primary text on panel ~12.7:1; primary text on well ~13.9:1; muted text on panel ~9.2:1; brass on panel ~4.9:1. Do not place dark text on a dark slot or white text on a bright button. Text must have 10px minimum horizontal breathing room, centered buttons, anti-overflow measures (`TMP` auto-sizing 13–18 and ellipsis), and rows tall enough for wrapped text. Native font preservation is non-negotiable.

## Required visual passes before acceptance

The existing `UI Smoke + Screenshots` harness is the entry point and must be run **after** this theme changes. Capture every element's **full screen, full panel, isolated control, and after-interaction** views. At minimum review: header/text/button, checkbox, slider/input/number, dropdown expanded, multi-page search, signal source + exposed property, swatches, tabs, columns, collapse/reorder, confirmations, property, live telemetry, tooltip, scrolling, *every machine-specific panel*, and native game/mod Settings screens.

For **each** snapshot, a person must review at both full size and thumbnail/contact sheet: 1) visual match to Options (dark plum + gold trim + game font); 2) label/readout contrast; 3) overlapping/oversized/truncated text; 4) exact control alignment/padding; 5) dropdown list appearing above other windows correctly; 6) no green machine-button textures bleeding into the settings panel; 7) panel title and first row visible on every reopen; 8) functional interactions; 9) clipping at 1600x1000 and at a narrower resolution; 10) UI cleanup after entering/leaving kingdom. Fix issues, regenerate snapshots, and repeat. An automated PASS is not a visual approval.

## Preservation and compatibility

- Keep `Foundation.dll` as the **only production DLL**.
- Do not introduce a new BepInEx plugin ID to implement themes.
- Keep existing public UI registry names and signatures; the native theme is an internal rendering detail.
- Do not modify source assets or extracted game files; the original game provides sprites at runtime.
- Do not discard or rewrite the original `ModConfigTemplates` and native Options layout.
- Use the pinned asset hash to identify which game visual baseline the review covered.
- If a native asset is unavailable, use the game-measured contrast-safe fallback; never generate a foreign-looking skin silently.

## Verification status

The original screenshot archive measured **36 PASS / 1 FAIL** (tooltip probe defect), before the prior source visual hotfix and before this native-asset theming pass. The source corrections here are **not yet compiled or rendered in game**. A layout geometry checker now logs potential label overlaps, text outside the panel, or unexpected font size; these are **review flags**, not a replacement for human visual inspection. It would be misleading to call this theme 100% finished without the new captured screenshots and interaction/regression results.
