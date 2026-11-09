# Signals and Property Bindings

Foundation signals are mod-defined typed properties and native-backed read-only sources. They are **not** the game's item pipes.

## Discover

```csharp
var sources = FoundationApi.SignalDirectory.Find("power");
var properties = FoundationApi.SignalDirectory.FindProperties("example.source");
```

Find properties by source ID, type and whether they support writes.

## Bind

```csharp
var link = FoundationApi.SignalLinks.Connect(
    "example.signals",
    new SignalSelection("example.source", "output"),
    new SignalSelection("example.destination", "input"));
```

Endpoints must exist, types must match and the destination must be writable. Inspect `link.IsResolved` and `link.UnavailableReason` if its provider is absent.

## Persist

`FoundationApi.SignalLinks.ExportOwner(ownerId)` returns serializable selection IDs. Store them with your existing `FoundationApi.ModSave` payload and restore using `ImportOwner` after registering providers.

Dispose link handles or use `RemoveOwner` on plugin shutdown. Bindings do not create `FlowPipe` connections.
