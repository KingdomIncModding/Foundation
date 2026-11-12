# Foundation SDK

The Foundation SDK helps mod developers extend **Kingdom Inc** with BepInEx 5 and the single `Foundation.dll` runtime. Target `netstandard2.1`, keep a stable plugin ID, and declare `[BepInDependency("urakka.kingdominc.foundation")]`.

## Main APIs

- `FoundationApi` — game services, content, UI, assets, signals and persistence.
- `FoundationMachineApi` — native machine discovery and specialized operations.
- `FoundationCoreApi.Core` — modules, dependencies, shared services and events.
- `FoundationApi.ForMod(modId)` — owned registrations and cleanup.

See [Getting Started](../Docs/Getting-Started.md), the [API reference](../Docs/API-Reference.md), [Game API](../Docs/GameAPI/README.md), [UI](../Docs/UI/README.md), [Core](../Docs/Core/README.md) and [Content](../Docs/Content-And-Persistence.md). Complete plugin examples are in [Examples](../Examples/).

To add an Options tab with native controls, see [Options tabs and controls](../Docs/UI/Options-Tabs.md).

## Building a packaged example

Extract the SDK into a development folder. It includes `lib/Foundation.dll`, XML IntelliSense documentation, the complete guides, and five example projects. Install the matching Foundation runtime separately in the game; the SDK is a development download.

Use .NET SDK 8 or later and point `GameRoot` to your own Kingdom Inc installation:

```powershell
dotnet build Examples/ExampleTechNodes/ExampleTechNodes.csproj -c Release "-p:GameRoot=C:/Games/KingdomInc/"
```

The other projects are `ExampleServices`, `ExampleGameplay`, `CoreMod/ExampleCoreMod.csproj`, and `UIDemo/Foundation.UIDemo.csproj` under `Examples`. Each references the SDK's `lib/Foundation.dll`. BepInEx and game/Unity references come from your game installation and are not bundled.

Do not distribute game-provided DLLs or copyrighted Kingdom Inc assets with your mod.
