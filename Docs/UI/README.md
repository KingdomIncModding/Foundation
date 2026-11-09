# Foundation 0.10.3 — Full UI Framework source implementation

**Build state:** source integrated with unified Core + Machines. This artifact was inspected against the user-supplied Kingdom Inc source, but the new C# code has **not** yet been compiled against installed Unity assemblies or exercised in-game. Keep the verified Foundation 0.10.2 DLL as your rollback until those steps pass.

## Goal and scope

Foundation exposes one extensible, native-themed UI framework to mod authors, without creating another production DLL and without replacing the Kingdom Inc UI system. New native-panel APIs are **additive**: existing `FoundationApi.ModBar`, `FoundationApi.ModConfig`, and `FoundationApi.UI` remain unchanged.

## Feature matrix — implementation in source

| Requested feature | Implementation | Runtime validation |
| --- | --- | --- |
| Native-themed panels | `FoundationPanelHost`, native `GameUI.TogglePopupMenu`, copied native popup/button/font | Pending |
| Buttons | `IFoundationPanelBuilder.Button`, native `ModBarContext` | Pending |
| Toggles | `Toggle` with native checkbox | Pending |
| Sliders | `Slider` with native button image styling and clamped step | Pending |
| Dropdowns | `Dropdown` clones the native TMP dropdown | Pending |
| Color swatches | `Colors`, rectangles and selected outline, no color wheel | Pending |
| Signal source and property | `Signal` with native source and exposed-property selectors; listens to registry changes | Pending |
| Numeric input | `Number`, invariant parsing and finite/range/step validation; Options `ModConfigNumber` staged Apply | Pending |
| Text input | `Input`, native font/frame TMP input; Options `ModConfigInput` staged Apply | Pending |
| Searchable selection | `Search` with 24 reusable page slots over the full catalog; Options `ModConfigSearch` | Pending |
| Tabs | `Tabs`, keyed tab selection persisted in PlayerPrefs per owner and panel | Pending |
| Collapsible sections | `Section`, keyed expanded state persisted | Pending |
| Tooltips | `FoundationPanelTooltip` uses footer on hover | Pending |
| Popups | `FoundationApi.Panels.Open`, one active native `UIPopupMenu` through `GameUI` | Pending |
| Confirmation dialogs | `Confirm` temporarily renders Yes/Cancel with user action callback | Pending |
| Drag and drop | `FoundationPanelReorder` row-to-row drag/drop | Pending |
| Reusable layouts | Header, row, columns, tabs, section, scrolling, modal dialog | Pending |
| Machine configuration panels | `TryResolveMachine` + `OpenMachine`; native default panels for 18 machine base/special types | Pending |
| Dynamic property editors | `FoundationUiProperty` for bool, number, text, signal and readonly; no reflection-based mutation | Pending |
| UI state persistence | `UiPanelState`, per-mod/panel tabs, sections, panel placement (local `PlayerPrefs`, not kingdom save) | Pending |
| Live telemetry | `LiveText` refreshes getters on a throttled frame cadence | Pending |

The existing ModBar/ModConfig have already passed their earlier Core/UI integration checks under Foundation 0.10.2. They must be regression-tested with this candidate.

## Public API

The single Foundation entry point is `KingdomFoundation.Api.FoundationApi.Panels` (`IFoundationPanelRegistry`). It supports owner-scoped registration and independent machine-panel resolution.

```csharp
using System;
using Assets.Source.Flow.Elements;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;

private IFoundationPanelModContext ui;

private void Awake()
{
    ui = FoundationApi.Panels.ForMod("my.mod.guid");
    ui.Register("preferences", "My Mod", builder =>
    {
        builder.Header("My Mod");
        builder.Toggle("Enabled", true, value => SaveEnabled(value));
        builder.Number("Threshold", 10, 1, 100, 1, value => SaveThreshold(value));
    });
    ui.Register("switch", "Switch Control", builder =>
    {
        var machine = (TrackSwitch)builder.Machine;
        builder.Toggle("Route to output 0", machine.SwitchPosition, value =>
        {
            var result = FoundationMachineApi.Operations.SetSwitch(machine, value);
            if (result.Succeeded) builder.Rebuild();
            else builder.Message(result.Message);
        });
    }, typeof(TrackSwitch), priority: 100);
}

private void OnDestroy() => ui?.Dispose();
```

When a mod wishes to show an existing machine's panel, call `FoundationApi.Panels.OpenMachine(placedNativeElement)` on Unity's main thread. Generic mod panels open with `ui.Open("preferences")` or `FoundationApi.Panels.Open(modId, panelId)`.

The first resolved match uses **most specific native machine type**, then descending priority, then lexically stable `(ModId, Id)` tie-breaking. The Foundation-provided base panels register at priority `-1000`; a consumer's priority `0` override wins for an equal type. Machine definitions are not openable as generic windows without a valid native machine.

## Lifecycle and safety

- The existing `FoundationPlugin` initializes default UI registrations after Core/Machine systems; no second production BepInEx plugin or Harmony owner.
- GameUI owns active popup, native close, and Escape behavior. The panel host is destroyed at Foundation shutdown or Unity scene teardown.
- A machine panel opens only for a live, placed native machine; it closes on removal.
- UI registration tokens and `IFoundationPanelModContext` must be disposed by the mod; disposing active registrations closes them.
- User callbacks are wrapped, and failures are reported to Foundation diagnostics instead of throwing into native game UI input loops.
- Built-in machine controls delegate to `FoundationMachineApi.Operations` or open the game's own selection menu. No synthetic production/IO game simulation was added.
- The store for panel layout/tabs/sections uses local `PlayerPrefs` only; it does not touch native kingdom saves or duplicate gameplay persistence.
- The client cannot impersonate another mod's stored owner identity through a cryptographic isolation boundary; registries establish *logical* ownership, not a security sandbox.

## Native default machine panels

`FlowElement` overview + I/O + telemetry fallback, `TrackSwitch`, `SingleButton`, `Timer`, `Refiner`, `RecipeElement`, `ResourceElement`, `Forest`, `Filter`, `Stockpile`, `Collector`, `Distributor`, `Manifold`, `Portal`, `Armory`, `QuestBoard`, `Dungeon`, and `RequisitionOffice`. Other native or mod-created `FlowElement` subtypes receive the base fallback unless the owning mod registers its own configuration panel.

## Install and test

1. **Back up** the working 0.10.2 `Foundation.dll`, source tree, and a disposable kingdom. Exit Kingdom Inc.
2. Merge this source archive into your project root: `KingdomInc\_Mods\Foundation\`. Use the source overlay ZIP if you want only changed files.
3. Run `INSTALL-FOUNDATION.cmd -BuildOnly -UseInstalledBepInExOnly` first for an isolated compile check. In a normal Windows console, use `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Install\Install-Foundation.ps1 -BuildOnly -UseInstalledBepInExOnly`.
4. After a successful compile, run `INSTALL-FOUNDATION.cmd` to install the **one unified production DLL** with backup. Do not install the independent demo as a player release.
5. For visual and interaction testing, run `Examples\UIDemo\INSTALL-UI-DEMO.cmd` (a **development-only** second DLL). Launch Kingdom Inc, load a disposable kingdom and use the new Mod Bar buttons **Test UI** and **Machine UI**.
6. Click each control; verify click sounds, native theme, available signal sources/properties, text/numeric validation, tab/section persistence, row reordering, drag-to-move, close/Escape, confirmation, save/reload with a native machine, and cleanup. Take screenshots or supply logs when anything differs from the game theme.
7. Run native isolated smoke checks and repeat Core 320/320 and Machine 26/26 suites against the installed 0.10.3 DLL; the extra panel registry source smoke checks are in `FoundationPlugin.PanelChecks.cs`. These checks require actual Windows runtime evidence, not merely static package integrity.
8. Close the game and run `Examples\UIDemo\REMOVE-UI-DEMO.cmd`. The final player installation contains **one `Foundation.dll`**.

### Static checks

`python Tests/verify_source.py` and `python Tests/verify_ui_source.py <extracted-KingdomIncSrc/Source>` verify source shape, contracts, API integration, no duplicate production plugin, native method anchors, and presence of all 21 components. These are **not C# compiler tests**.

### Release gate (not yet met)

Do **not** describe this as 100% verified until the 0.10.3 Windows build succeeds, the demo UI is visually approved in-game, UI interactions pass, and the Core + Machine regression suites pass. A source implementation can be substantially complete without proving runtime behavior.
