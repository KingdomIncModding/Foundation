# Foundation Core

Foundation Core provides common infrastructure for mod registration, dependencies, lifecycle, services and inter-mod communication.

```csharp
using KingdomFoundation.Core;

var core = FoundationCoreApi.Core;
```

Core exposes `Modules`, `Services`, `Events`, `Patches`, `Plugins`, `Content`, `Mods` and `ServiceDirectory`.

## Modules

A mod implements `IFoundationModule` and registers it with the shared catalog. The registration is owned by the mod and should be disposed on shutdown.

```csharp
private IModRegistration registration;
private void Awake()
{
    registration = FoundationCoreApi.Core.Modules.Register(new ExampleModule());
}
private void OnDestroy() => registration?.Dispose();
```

The example assumes an `ExampleModule` implementation; see [Examples/CoreMod](../../Examples/CoreMod/) for the full plugin.

## Services between mods

A provider publishes a service under a stable ID and shared interface. Consumers discover it without depending on the provider's implementation DLL.

```csharp
bool found = FoundationCoreApi.Core.Services.TryResolve<IMyService>(
    "example.provider:service", out var service);
```

Core manages ownership and lifetimes. Unity objects must be accessed on the main thread; use the Core dispatcher for background-to-main-thread work.

Simple mods can start with `FoundationApi.ForMod(modId)` and do not have to implement a Core module.
