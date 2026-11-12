# Creating Options tabs

Requires **Foundation 0.12.0 or later**, BepInEx 5 and .NET Standard 2.1. Everything below runs on Unity's main thread.

Foundation supplies **Game**, **Mods** and **Installed** tabs. Your mod can register additional top-level tabs between Game and the right-aligned Mods/Installed group. Titles determine tab widths, with consistent padding and centered text. The left tab area scrolls horizontally when necessary; drag or use the mouse wheel over it.

Foundation does not create Audio or Display tabs and does not implement an audio manager.

## A complete tab with controls

Reference the installed `Foundation.dll`, BepInEx, Assembly-CSharp, UnityEngine/CoreModule, UnityEngine.UI and Unity.TextMeshPro with `Private=false`. This plugin uses a dedicated tab settings section; it does not also add those controls to the Mods page.

```csharp
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;

[BepInPlugin(Id, "Workshop", "1.0.0")]
[BepInDependency("urakka.kingdominc.foundation", "0.12.0")]
public sealed class WorkshopPlugin : BaseUnityPlugin
{
    public const string Id = "example.workshop";
    private FoundationModContext mod;

    private void Awake()
    {
        ConfigEntry<bool> enabled = Config.Bind("General", "Enabled", true);
        ConfigEntry<float> intensity = Config.Bind("General", "Intensity", 0.5f);
        ConfigEntry<string> mode = Config.Bind("General", "Mode", "Normal");
        ConfigEntry<string> label = Config.Bind("General", "Label", "My workshop");
        ConfigEntry<int> limit = Config.Bind("General", "Limit", 10);

        mod = FoundationApi.ForMod(Id);
        var tab = new OptionsTab(Id, "preferences", "Workshop", order: 10);
        IModConfigSection controls = tab.Settings;

        controls.AddText("Workshop settings", isHeader: true);
        controls.AddText("Configure your workshop here.");
        controls.AddToggle(enabled, "Enabled");
        controls.AddSlider(intensity, 0f, 1f, step: 0.05f,
            customLabel: "Intensity", formatValue: value => $"{value:P0}");
        controls.AddDropdown(mode, new[] { "Normal", "Detailed", "Compact" }, "Mode");
        controls.AddInput("Label", label.Value,
            value => { label.Value = value; Config.Save(); },
            getter: () => label.Value);
        controls.AddNumber("Limit", 0, 100, limit.Value, 1,
            value => { limit.Value = (int)value; Config.Save(); },
            getter: () => limit.Value);
        controls.AddButton("Actions", () => Logger.LogInfo("Action clicked"), "Run action");

        mod.Register(tab);
    }

    private void OnDestroy() => mod?.Dispose();
}
```

Register during plugin initialization, even before Options exists, or register later while the menu is open. Add controls before registering when practical. Adding/removing elements afterwards refreshes the page without discarding staged values.

## Interfaces and registration lifetime

| Contract | Responsibility |
| --- | --- |
| `IOptionsTabRegistry` | Register, enumerate and look up tabs; observe Options views. Available as `FoundationApi.Options`. |
| `IOptionsTab` | Supply mod GUID, local ID, title, order and an `IModConfigSection`. |
| `IOptionsTabRegistration` | Dispose the tab or request a presentation refresh. |
| `IOptionsView` | Access one menu instance's vanilla rows. |
| `IVanillaSettingView` | Inspect row lifetime and change its current presentation visibility. |
| `IModConfigElement` | Implement custom controls, including view creation, Commit and Cancel. |

`OptionsTab` is a convenience implementation, not a required base class. You can implement `IOptionsTab` yourself and supply a section from `new ModConfigRegistry().GetOrCreateSection(yourModId, title)`. The menu renders every tab through its interfaces.

For direct registration without `FoundationModContext`:

```csharp
IOptionsTabRegistration handle = FoundationApi.Options.Register(tab);
// Dispose this handle when your plugin shuts down.
handle.Dispose();
```

IDs are unique per owning GUID. Different mods may reuse a local ID. Tabs sort by Order, then mod GUID and local ID. Mod ID, local ID and settings-section identity cannot change after registration. A custom implementation can change its Title/Order and call `handle.Refresh()`. Disposing is idempotent; removing the selected tab returns to Game. Disposing a mod context removes tabs registered through that context.

## Apply, Cancel and custom controls

- Switching pages preserves pending edits across Game, Mods and custom tabs.
- Apply & Close commits participating sections once, including a section shared by multiple tabs.
- Cancel, the close button and leaving Options discard unapplied values. Removing a tab during editing cancels its section if no remaining page uses it.
- Toggle, slider, dropdown, text-input and numeric-input changes are staged. ConfigEntry helpers write/save only when committed.
- Action buttons run immediately when clicked; Cancel does not reverse an action.
- Custom controls implement `IModConfigElement`. Create their Unity UI under `context.Container` using `context.Templates`, keep pending state in the element rather than its scene objects, and implement Commit/Cancel. Preserve existing control contracts when extending them.

## Let your mod hide a vanilla setting

Foundation starts each Options opening with all vanilla rows visible. It does not save a hidden-settings policy or infer hiding from your tab name. Your mod explicitly changes the view while present.

This example only hides a row. It does not change volume or implement audio features. Add these members to your plugin if it needs to replace a native row with its own controls:

```csharp
private System.IDisposable viewObserver;
private readonly System.Collections.Generic.List<IVanillaSettingView> hiddenRows =
    new System.Collections.Generic.List<IVanillaSettingView>();

private void ObserveOptions()
{
    viewObserver = FoundationApi.Options.ObserveViews(view =>
    {
        hiddenRows.RemoveAll(row => !row.IsAlive);
        if (!view.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var row))
            return;

        row.IsVisible = false; // Your mod makes this decision.
        if (!hiddenRows.Contains(row)) hiddenRows.Add(row);
    });
}

private void StopObservingOptions()
{
    viewObserver?.Dispose();
    foreach (var row in hiddenRows)
        if (row.IsAlive) row.IsVisible = true;
    hiddenRows.Clear();
}
```

Call `ObserveOptions()` from Awake and `StopObservingOptions()` from OnDestroy before disposing the mod context. Observation runs immediately for already-open menus and on each subsequent opening. The mod restores its own presentation changes if it unloads during the current session. If the mod is absent next launch, nothing hides the native settings and they appear normally.

Use `IsAlive` before keeping a view reference across scene changes. Foundation handles layout reflow when a mod changes IsVisible, including empty headings. Only row visibility changes: native values, saving and gameplay remain intact. There is no ownership arbitration between mods changing the same row; those mods must coordinate their presentation choices.

### Stable vanilla IDs

Use the matching constants on `VanillaSettingIds`:

| ID | Native row |
| --- | --- |
| `Resolution` | Resolution dropdown |
| `WindowMode` | Windowed/fullscreen toggle |
| `Framerate` | Framerate limit |
| `UIScale` | Interface scale |
| `MusicVolume` | Music volume |
| `SoundVolume` | Effects volume |
| `AutosaveFreq` | Autosave interval |
| `KeyBindings` | Key bindings button |

Existing `FoundationApi.ModConfig` / `mod.Settings(...)` registrations continue to appear in the Mods tab. The new tab API does not require older mods to change their settings registrations.
