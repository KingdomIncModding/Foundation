# Native Pipes, Ports and Throughput

Kingdom Inc owns its `FlowIO` and `FlowPipe` transport graph. Foundation provides typed access to the real connections, native restrictions and observed item movement.

```csharp
using KingdomFoundation.Machines;
using Assets.Source.Flow.Elements;

var io = FoundationMachineApi.IO;

foreach (var collector in FoundationMachineApi.Registry.OfType<Collector>())
{
    var ports = io.Ports(collector);
    var routing = io.Router(collector);
    var traffic = io.Throughput(collector);
}
```

`FoundationApi.IO` exposes native item-pipe operations; `FoundationApi.NativeIO` and `FoundationMachineApi.IO` provide machine-oriented inspection and routing.

Throughput here means **observed transfers**, not an invented fixed capacity. Connection behavior must satisfy the actual game's item compatibility, buffering, direction and machine-specific rules.

Foundation's [signals](../Signals.md) are independent typed property connections. They do not create pipes or move inventory items.
