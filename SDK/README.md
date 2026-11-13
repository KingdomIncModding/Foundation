# Foundation SDK

The Foundation SDK helps mod developers extend **Kingdom Inc** with BepInEx 5 and the single `Foundation.dll` runtime. Target `netstandard2.1`, keep a stable plugin ID, and declare `[BepInDependency("urakka.kingdominc.foundation")]`.

## Main APIs

For consumers that must avoid a direct game-assembly dependency, start with [the framework boundary](../Docs/Framework-Boundary.md). Foundation owns native hooks and primitive game adapters; feature behavior remains in the mod. Existing native-facing APIs remain available for compatibility during migration.

- `FoundationApi` — game services, content, UI, assets, signals and persistence.
- `FoundationMachineApi` — native machine discovery and specialized operations.
- `FoundationCoreApi.Core` — modules, dependencies, shared services and events.
- `FoundationApi.ForMod(modId)` — owned registrations and cleanup.

See [Getting Started](../Docs/Getting-Started.md), the [API reference](../Docs/API-Reference.md), [Game API](../Docs/GameAPI/README.md), [UI](../Docs/UI/README.md), [Core](../Docs/Core/README.md) and [Content](../Docs/Content-And-Persistence.md). Complete plugin examples are in [Examples](../Examples/).

Foundation **0.14.0** and this matching SDK target **Kingdom Inc game build 1.0.14**. Use `FoundationApi.Economy.ExchangeTaxDouble(currency)` for native precision. The old float-returning `ExchangeTax(currency)` remains available with an `[Obsolete]` warning identifying **game build 1.0.13** as its last native signature and **the next Foundation release** as its removal point. See the [migration and deprecation register](../Docs/Migration.md#4-kingdom-inc-game-build-1014) before updating a consumer.

To add an Options tab with native controls, see [Options tabs and controls](../Docs/UI/Options-Tabs.md).

## Building a packaged example

Extract the SDK into a development folder. It includes `lib/Foundation.dll`, XML IntelliSense documentation, the complete guides, and six example projects. Install the matching Foundation runtime separately in the game; the SDK is a development download.

Use .NET SDK 8 or later and point `GameRoot` to your own Kingdom Inc installation:

```powershell
dotnet build Examples/ExampleTechNodes/ExampleTechNodes.csproj -c Release "-p:GameRoot=C:/Games/KingdomInc/"
```

Start new consumers with `Examples/FrameworkConsumer/FrameworkConsumer.csproj`: it has no game-assembly or Harmony reference, and demonstrates a mod-owned machine with a Foundation-owned native host. The five earlier projects remain as compatibility examples: `ExampleTechNodes`, `ExampleServices`, `ExampleGameplay`, `CoreMod/ExampleCoreMod.csproj`, and `UIDemo/Foundation.UIDemo.csproj`. Each references the SDK's `lib/Foundation.dll`. BepInEx and Unity dependencies come from your game installation and are not bundled; the earlier examples also exercise native-facing legacy APIs.

Do not distribute game-provided DLLs or copyrighted Kingdom Inc assets with your mod.
