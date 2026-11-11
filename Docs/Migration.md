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
