# Migrating Existing Mods

The standard runtime is a single `Foundation.dll`.

## 1. Update Assembly References

If a mod referenced `Foundation.Machines.dll`, change the reference to `Foundation.dll` and recompile. The machine API remains in the `KingdomFoundation.Machines` namespace, but moving a type to a different assembly changes its CLR identity.

A regular Foundation plugin uses:

```csharp
[BepInDependency("urakka.kingdominc.foundation", ">=0.11.0")]
```

Core modules can depend on `urakka.kingdominc.foundation.core` where appropriate.

## 2. Adopt the Standardized Mod Structure & `mod.json`

1. Place all C# source files under `src/` and keep `.csproj`, `mod.json`, and `README.md` at the mod root.
2. Create a valid `mod.json` in your mod root containing your `id`, `name`, `version`, `author`, `repository`, and `distribution.nexus` (`game` and `modId`). See [Getting Started](Getting-Started.md) for the complete schema.
3. **Copy `mod.json` with the Release DLL**:
   - Whenever you build or package a release, copy `mod.json` alongside your compiled `{ModName}.dll` into `{ModName}/Release/`, `BepInEx/plugins/{ModName}/`, and your release `.zip` archive (`BepInEx/plugins/{ModName}/mod.json`).
   - Foundation reads `mod.json` directly from the plugin folder to automatically check for updates on Nexus Mods v3 without requiring API keys or hardcoded mod IDs.

## 3. Preserve Save & Content Compatibility

Do not rename existing plugin GUIDs, item and recipe IDs, research IDs, workspace node IDs, signal IDs, or save keys simply because the framework changed. Saved kingdoms may contain those references.

For owned registrations, prefer `FoundationApi.ForMod(modId)` and dispose it in `OnDestroy`.

`IFoundationCore` includes `Mods` and `ServiceDirectory` getters. Mods implementing this interface themselves need to provide these members; most mods should consume `FoundationCoreApi.Core` instead.

Use `FoundationApi.Panels` and `ModBar` for UI, `SignalDirectory` / `SignalLinks` for bindings, and `ModSave` for additional versioned data.

## 4. Kingdom Inc game build 1.0.14

Build mods against the 1.0.14 game assemblies and the matching Foundation 0.13.0 runtime/SDK. Game build numbers below refer to Kingdom Inc, not Foundation's version.

| Retained deprecated API | Last working native game build | Replacement | Removal |
| --- | --- | --- | --- |
| `IGameEconomyService.ExchangeTax(Currency)` returning `float` | **1.0.13** | `ExchangeTaxDouble(Currency)` returning `double` | Next Foundation release |

The old method keeps its exact CLR signature and forwards to the current game with an explicit float conversion. Existing consumers remain callable; newly compiled uses receive a non-error `[Obsolete]` warning. Use the replacement to preserve native precision and the game's five-percent minimum tax. Custom implementations of `IGameEconomyService` must also implement the new method.

```csharp
double tax = FoundationApi.Economy.ExchangeTaxDouble(currency);
```

Timers no longer require `outfitter_timeout` research: the machine command, capability list, and default panel follow the updated game. Outfitter timeout configuration still requires its own research. Both switch APIs refresh the native child control, including its new background sprites.

Game 1.0.14 snapshots contain `Version` and `Player`, plus Foundation's mod-data extension; they no longer create `Secret`. Old saves that contain `Secret` still load. The internal legacy checksum adapter is also marked `[Obsolete]`, records **1.0.13**, and is scheduled for removal next release. Remove both compatibility adapters and their explicit diagnostic warning suppressions together when making that release; keep loading older native saves.

Foundation does not expose wrappers named `TrackSwitch.UpdateTrackSwitch` or `Tier10._restoreEnergy`. If your mod calls or patches the game directly, the former now takes `bool newTech = false`, and the latter has become `_restoreEnergyScroll`. Reflection/Harmony lookups for `UpdateTrackSwitch` must specify `typeof(bool)`; an optional C# parameter does not retain the old parameterless CLR method. Currency exchange UI amounts and `SliderMax()` now use `long`.
