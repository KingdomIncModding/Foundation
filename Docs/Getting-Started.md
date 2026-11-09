# Getting Started with Foundation

A Foundation mod is a standard BepInEx 5 C# plugin targeting `netstandard2.1`. Reference the unified `Foundation.dll` and whichever actual Kingdom Inc / Unity assemblies your mod uses. Install Foundation at `BepInEx/plugins/Foundation/`.

## Your first mod

```csharp
using BepInEx;
using KingdomFoundation.Api;

[BepInPlugin("example.myfoundationmod", "My Foundation Mod", "1.0.0")]
[BepInDependency("urakka.kingdominc.foundation")]
public sealed class MyFoundationMod : BaseUnityPlugin
{
    private FoundationModContext context;

    private void Awake()
    {
        context = FoundationApi.ForMod("example.myfoundationmod");

        var settings = context.Settings("My Mod", "My Foundation preferences");
        settings.AddToggle("Enabled", true,
            enabled => Logger.LogInfo("Enabled: " + enabled));

        context.Listen<WorkspaceChangedEvent>(change =>
            Logger.LogInfo("Workspace changed: " + change.Kind));
    }

    private void OnDestroy() => context?.Dispose();
}
```

The context groups your subscriptions and other disposable registrations by stable mod ID.

## Choose the right entry point

- `FoundationApi`: gameplay services, content, UI, signals and mod data.
- `FoundationMachineApi`: native machine registry, capabilities and operations.
- `FoundationCoreApi.Core`: modules, dependencies, services and inter-mod events.

Register permanent game-backed content before loading a kingdom. Work with live Unity objects on the main thread, and do not keep native machine references after their world unloads.

More examples: [research](../Examples/ExampleTechNodes/), [services](../Examples/ExampleServices/), [Core modules](../Examples/CoreMod/), [gameplay](../Examples/ExampleGameplay/) and [UI](../Examples/UIDemo/).
