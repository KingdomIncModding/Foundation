# Foundation SDK

The Foundation SDK helps mod developers extend **Kingdom Inc** with BepInEx 5 and the single `Foundation.dll` runtime. Target `netstandard2.1`, keep a stable plugin ID, and declare `[BepInDependency("urakka.kingdominc.foundation")]`.

## Main APIs

- `FoundationApi` — game services, content, UI, assets, signals and persistence.
- `FoundationMachineApi` — native machine discovery and specialized operations.
- `FoundationCoreApi.Core` — modules, dependencies, shared services and events.
- `FoundationApi.ForMod(modId)` — owned registrations and cleanup.

See [Getting Started](../Docs/Getting-Started.md), the [API reference](../Docs/API-Reference.md), [Game API](../Docs/GameAPI/README.md), [UI](../Docs/UI/README.md), [Core](../Docs/Core/README.md) and [Content](../Docs/Content-And-Persistence.md). Complete plugin examples are in [Examples](../Examples/).

Do not distribute game-provided DLLs or copyrighted Kingdom Inc assets with your mod.
