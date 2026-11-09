# Foundation UI

Foundation provides native-themed controls for mod settings, HUD entries, custom panels and machine-specific interfaces.

## Register a panel

```csharp
using KingdomFoundation.Api;

private IFoundationPanelModContext panels;

private void Awake()
{
    panels = FoundationApi.Panels.ForMod("example.ui");

    panels.Register("preferences", "My Mod", ui =>
    {
        ui.Header("My Mod");
        ui.Toggle("Enabled", true, enabled => SetEnabled(enabled));
        ui.Number("Threshold", 10, 1, 100, 1, value => SetThreshold(value));
        ui.Input("Name", "Example", value => SetName(value));
    });
}

private void OnDestroy() => panels?.Dispose();
```

The example assumes the callbacks are supplied by your mod. Open the registered panel with `panels.Open("preferences")`.

## Available UI features

The panel builder supports buttons, toggles, sliders, inputs, dropdowns, searchable selection, rectangular color swatches, signal selectors, tabs, sections, confirmations, draggable rows and live values. See [Examples/UIDemo](../../Examples/UIDemo/).

`FoundationApi.ModBar` registers HUD controls; `FoundationApi.ModConfig` integrates configuration with the game's settings. `FoundationApi.Panels.OpenMachine(machine)` opens a matching panel for a live placed machine.

Dispose registration handles when your mod shuts down. For machine operations, delegate to the game's native API.
