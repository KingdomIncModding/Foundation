# Native Machines

Kingdom Inc uses native `FlowElement` classes. Foundation makes those objects discoverable and exposes their supported operations.

## Find machines

```csharp
using KingdomFoundation.Machines;
using Assets.Source.Flow.Elements;

var machines = FoundationMachineApi.Registry.All();
var collectors = FoundationMachineApi.Registry.OfType<Collector>();

foreach (var machine in machines)
{
    var snapshot = FoundationMachineApi.Registry.Inspect(machine);
    var capabilities = FoundationMachineApi.Capabilities.SupportedCommands(machine);
}
```

`Registry.All()` returns live game objects. `Inspect()` captures state including inputs/outputs and native properties.

## Invoke a game-owned operation

```csharp
foreach (var collector in FoundationMachineApi.Registry.OfType<Collector>())
{
    var result = FoundationMachineApi.Operations.AdvanceCollector(collector);
    if (!result.Succeeded)
        UnityEngine.Debug.LogWarning(result.Message);
}
```

Other supported operations cover native crafting recipes, resource selection, filtering, switching, timing, storage, manual production and specialized game machines. Use `Capabilities.SupportedCommands` instead of assuming every operation applies to every machine.

See [Logistics](Logistics.md) for pipe and port APIs and [UI](../UI/README.md) for machine-specific controls.
