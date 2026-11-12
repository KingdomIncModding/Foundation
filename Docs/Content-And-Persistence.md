# Custom Content and Saved Data

Foundation supports mod-created content through the native game registries, with separate data storage for values the game doesn't already save.

## Content registration

`FoundationApi.Content` registers supported native items, recipes, quests, dungeons, wars, prestige nodes and enhancements. Use `FoundationApi.TechNodes` for research definitions and `FoundationApi.WorkspaceNodes` for mod workspace nodes.

Register native-backed content early, before a kingdom loads. Keep mod GUIDs and content identifiers stable: saved kingdoms may reference their IDs even while the provider mod is unavailable.

## Additional per-kingdom data

```csharp
using LightJson;
using KingdomFoundation.Api;

var registration = FoundationApi.ModSave.Register(
    "example.myplugin", 1,
    save: () => new JsonObject { { "enabled", true } },
    load: (savedVersion, state) =>
    {
        // Restore this mod's fields and handle older schema versions.
    });
```

Hold the returned `IDisposable` and release it on shutdown. `FoundationApi.SaveMigrations` exposes migration support for evolving schemas.

`FoundationApi.Assets` and `FoundationApi.AssetDirectory` manage mod-owned sprites and other assets. Keep assets alive while registered content or UI uses them.

Store stable IDs rather than live Unity object references.

Game 1.0.14 no longer writes a `Secret` checksum. Foundation adds its data to the native document without introducing that field. Older saves containing `Secret` remain loadable; subsequent snapshots use the current native format. See [Migration](Migration.md) for the retained legacy adapter and removal schedule.
