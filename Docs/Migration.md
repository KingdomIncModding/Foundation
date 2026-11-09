# Migrating Existing Mods

The standard runtime is a single `Foundation.dll`.

## Update assembly references

If a mod referenced `Foundation.Machines.dll`, change the reference to `Foundation.dll` and recompile. The machine API remains in the `KingdomFoundation.Machines` namespace, but moving a type to a different assembly changes its CLR identity.

A regular Foundation plugin uses:

```csharp
[BepInDependency("urakka.kingdominc.foundation")]
```

Core modules can depend on `urakka.kingdominc.foundation.core` where appropriate.

## Preserve compatibility

Do not rename existing plugin GUIDs, item and recipe IDs, research IDs, workspace node IDs, signal IDs or save keys simply because the framework changed. Saved kingdoms may contain those references.

For owned registrations, prefer `FoundationApi.ForMod(modId)` and dispose it in `OnDestroy`.

`IFoundationCore` includes `Mods` and `ServiceDirectory` getters. Mods implementing this interface themselves need to provide these members; most mods should consume `FoundationCoreApi.Core` instead.

Use `FoundationApi.Panels` and `ModBar` for UI, `SignalDirectory` / `SignalLinks` for bindings, and `ModSave` for additional versioned data.
