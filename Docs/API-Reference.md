# Foundation API Reference

The primary API is `KingdomFoundation.Api.FoundationApi`.

| Property or entry point | Purpose |
| --- | --- |
| `World`, `Workspace` | Current kingdom and workspace editing |
| `Machines`, `IO`, `NativeIO` | Game machines and native logistics |
| `Items`, `Production`, `Economy` | Items, crafting and currencies |
| `Progression`, `Adventures`, `Combat` | Research, quests, war and combat |
| `View`, `Audio`, `Presentation` | Camera/input, audio and native assets |
| `Content`, `TechNodes`, `WorkspaceNodes` | Supported mod-created definitions |
| `Panels`, `ModBar`, `ModConfig`, `NativeUiFactory` | Native-themed mod interfaces |
| `Signals`, `SignalDirectory`, `SignalLinks` | Signal source/property discovery and bindings |
| `Assets`, `AssetDirectory` | Mod-owned assets |
| `ModSave`, `SaveMigrations` | Per-kingdom mod-owned persistence |
| `Events`, `Lifecycle`, `Diagnostics` | Events, lifecycle and diagnostics |
| `ForMod(modId)` | Owner-scoped registration context (`FoundationModContext`) |

---

## FoundationModContext & ModMetadata

Obtained via `FoundationApi.ForMod(modId)`. Owns your mod's registrations and automatically disposes them when your plugin unloads.

| Member | Type | Purpose |
|---|---|---|
| `ModId` | `string` | The mod GUID passed during initialization. |
| `ModDirectory` | `string` | Absolute path to the mod's install folder (e.g. `BepInEx/plugins/MyMod/`). |
| `AssetsDirectory` | `string` | Absolute path to `Assets/` inside `ModDirectory`. |
| `Metadata` | `ModMetadata` | Parsed metadata from `mod.json` (`Name`, `Version`, `Author`, `Description`, `NexusGame`, `NexusModId`, etc.). |
| `GetAssetPath(cat, file)` | `string` | Returns absolute path to `Assets/{category}/{fileName}`. |
| `LoadAssetSprite(cat, file, ppu)` | `IFoundationAsset` | Loads a sprite from `Assets/{category}/{fileName}` with lifecycle tracking. |
| `Settings(title, desc)` | `IModConfigSection` | In-game mod settings section in Options -> Mods. |
| `Listen<T>(handler)` | `IDisposable` | Subscribes to typed game and Foundation events. |
| `EachFrame(callback)` | `IDisposable` | Runs a callback each Unity frame. |
| `BindKey(key, handler)` | `IDisposable` | Binds a keypress shortcut. |
| `Register(definition)` | `ITechNode` / `IWorkspaceNodeType` | Registers tech tree or workspace nodes. |
| `Register(element)` | `IModBarRegistration` | Registers buttons, toggles, or dropdowns in the top Mod Bar. |
| `Persist(...)` | `IDisposable` | Registers per-kingdom custom save data. |

---

## Additional Entry Points

- `KingdomFoundation.Machines.FoundationMachineApi`: machine-specific discovery, inspection, supported commands, and native IO.
- `KingdomFoundation.Core.FoundationCoreApi.Core`: module registration, dependency management, shared services, and events.

For exact signatures and argument types, see `src/Foundation/Api/` and `src/Foundation/Core/Contracts/`.
