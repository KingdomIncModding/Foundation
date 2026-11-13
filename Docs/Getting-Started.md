# Getting Started with Foundation

A Foundation mod is a standard BepInEx 5 C# plugin targeting `netstandard2.1`. Reference the unified `Foundation.dll`, BepInEx and the Unity assemblies your mod uses. Foundation 0.14.0 owns native game integration through its [framework contracts](Framework-Boundary.md). Start with the [FrameworkConsumer example](../Examples/FrameworkConsumer/FrameworkConsumer.csproj), which has no game-assembly or Harmony reference. Existing native-facing SDK APIs and their compatibility examples remain available. Install Foundation at `BepInEx/plugins/Foundation/`.

---

## 1. Standardized Mod Structure

Every Foundation mod follows a unified directory and project structure:

```text
_Mods/{ModName}/
├── mod.json                     # Primary mod manifest and metadata
├── {ModName}.csproj             # C# project file targeting netstandard2.1
├── README.md                    # Exact byte-for-byte mirror of Nexus/DESCRIPTION.md
├── .gitignore                   # Ignores bin/, obj/, _AI/, Nexus/, etc.
├── src/                         # All C# source code belongs here ONLY
│   └── ...                      # Subfolders and .cs files
├── Assets/                      # Approved runtime assets only
│   ├── Icons/                   # In-game UI icons
│   ├── Thumbnails/              # Mod thumbnails
│   └── Screenshots/             # Promo screenshots
├── Nexus/                       # Player-facing release documentation
│   ├── DESCRIPTION.md           # Mod page description (exact mirror of README.md)
│   ├── CHANGELOG.md             # Version history meaningful to players
│   ├── PREREQUISITES.md         # Prerequisites (Kingdom Inc, BepInEx 5, Foundation)
│   └── COMPATIBILITY.md         # Harmony hooks table (| Class.Method | Hook Type | Impact |)
└── _AI/                         # Local development notes, prompts, and scratchpads (gitignored)
```

---

## 2. Creating the `mod.json` File

The `mod.json` file is the authoritative single source of truth for your mod's identity, repository, dependencies, and Nexus distribution. Foundation automatically parses this file at runtime to power the in-game Mod Settings menu, update checks, and asset loading.

Place `mod.json` in your mod root directory beside your `.csproj`, and ensure it is copied alongside your compiled `.dll` inside `BepInEx/plugins/{ModName}/mod.json`.

### Template `mod.json`

```json
{
  "id": "MyMod",
  "name": "My Foundation Mod",
  "version": "1.0.0",
  "author": "YourName",
  "description": "A clean, player-facing summary of what your mod does in Kingdom Inc.",
  "repository": {
    "platform": "GitHub",
    "owner": "yourgithubusername",
    "name": "MyMod",
    "url": "https://github.com/yourgithubusername/MyMod",
    "branch": "main",
    "visibility": "public",
    "topics": [
      "kingdominc",
      "bepinex",
      "mod",
      "quality-of-life"
    ],
    "latestTag": "v1.0.0"
  },
  "distribution": {
    "nexus": {
      "game": "kingdominc",
      "modId": 0,
      "fileId": null,
      "url": "https://www.nexusmods.com/kingdominc/mods/0",
      "requirements": [
        {
          "name": "Foundation",
          "url": "https://www.nexusmods.com/kingdominc/mods/18",
          "version": ">=0.11.0",
          "optional": false
        },
        {
          "name": "BepInEx 5",
          "url": "https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6",
          "version": ">=5.4.23.6",
          "optional": false
        }
      ],
      "tags": [
        "Quality of Life",
        "Utilities for Players"
      ],
      "tagsVerified": true,
      "requirementsVerified": true
    }
  },
  "development": {
    "framework": "Foundation",
    "language": "C#",
    "target": "netstandard2.1",
    "dependencies": [
      "BepInEx 5",
      "Foundation >= 0.11.0"
    ]
  },
  "publishing": {
    "githubReleases": true,
    "nexusMods": true
  }
}
```

### Key Field Descriptions

| Field | Description |
|---|---|
| `id` | Stable identifier / BepInEx GUID (e.g. `MyMod` or `author.kingdominc.mymod`). |
| `name` | Human-readable display name shown in-game and on mod pages. |
| `version` | Semantic version string matching your assembly and release tag (e.g. `1.0.0`). |
| `author` | Creator name. |
| `description` | Concise summary of player-facing features. |
| `repository.url` | GitHub repository URL. |
| `distribution.nexus.game` | Nexus game domain (e.g. `kingdominc`). Foundation uses this dynamically—no hardcoded game IDs! |
| `distribution.nexus.modId` | Nexus Mod ID (e.g. `8`, `21`). Foundation uses this for automatic version checking against Nexus v3 without requiring an API key. |
| `distribution.nexus.requirements` | Structured prerequisite list linking Foundation and BepInEx. |

---

## 3. Writing Your Plugin

Use `FoundationApi.ForMod(...)` in your `Awake()` method to initialize a `FoundationModContext`. Foundation automatically loads your `mod.json` metadata and connects your mod to the framework.

```csharp
using BepInEx;
using KingdomFoundation.Api;
using UnityEngine;

namespace MyMod
{
    [BepInPlugin("example.mymod", "My Foundation Mod", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", ">=0.11.0")]
    public sealed class MyModPlugin : BaseUnityPlugin
    {
        private FoundationModContext context;

        private void Awake()
        {
            // Initializes context and automatically resolves mod.json metadata and asset directories
            context = FoundationApi.ForMod("example.mymod");

            // Access parsed metadata directly
            if (context.Metadata != null)
            {
                Logger.LogInfo($"Loaded {context.Metadata.Name} v{context.Metadata.Version} by {context.Metadata.Author}");
            }

            // Register in-game Mod Settings
            var settings = context.Settings("My Mod", "Preferences for My Mod");
            settings.AddToggle("Enabled", true,
                enabled => Logger.LogInfo("Toggled: " + enabled));

            // Load an approved runtime sprite from Assets/Icons/button.png
            // var spriteAsset = context.LoadAssetSprite("Icons", "button.png");

            // Listen for native game events
            context.Listen<WorkspaceChangedEvent>(change =>
                Logger.LogInfo("Workspace changed: " + change.Kind));
        }

        private void OnDestroy()
        {
            // Automatically cleans up all owned settings, listeners, and assets
            context?.Dispose();
        }
    }
}
```

---

## 4. How Foundation Uses `mod.json` at Runtime

1. **Automatic Nexus Update Checking**:
   Foundation reads `distribution.nexus.game` and `distribution.nexus.modId` from your `mod.json`. When the player opens the in-game **Installed Mods** menu, Foundation checks Nexus Mods v3 (`https://api.nexusmods.com/v3/games/{game}/mods/{modId}`) publicly without requiring any user API key.
2. **In-Game Mod Links**:
   Foundation builds clickable **Nexus Page** buttons directly from your `distribution.nexus` configuration.
3. **Asset Resolution**:
   `context.AssetsDirectory` automatically locates your `Assets/` folder whether running from a player's `BepInEx/plugins/` installation or a developer workspace. `context.LoadAssetSprite(category, fileName)` safely loads sprites with automatic memory lifecycle tracking.

---

## 5. Build, Release, and Installation

1. Put your source files under `src/`.
2. Ensure your `.csproj` targets `netstandard2.1` and references `Foundation.dll` from `BepInEx/plugins/Foundation/`.
3. Build in Release mode:
   ```powershell
   dotnet build -c Release
   ```
4. **Always copy `mod.json` with your Release DLL**:
   - Local Release folder: `{ModName}/Release/{ModName}.dll` and `{ModName}/Release/mod.json`
   - Game installation folder: `BepInEx/plugins/{ModName}/{ModName}.dll` and `BepInEx/plugins/{ModName}/mod.json`
5. When packaging a release ZIP for players, GitHub Releases, or Nexus Mods, always bundle `mod.json` alongside the release DLL:
   ```text
   {ModName}-v{Version}.zip
   └── BepInEx/
       └── plugins/
           └── {ModName}/
               ├── {ModName}.dll
               └── mod.json
   ```

---

## More Examples

Explore consumer mod projects in Foundation's [Examples/](../Examples/) directory:
- [ExampleTechNodes](../Examples/ExampleTechNodes/): Custom technology tree nodes and research unlocks.
- [ExampleServices](../Examples/ExampleServices/): Inter-mod services and dependency injection.
- [CoreMod](../Examples/CoreMod/): Core framework modules and lifecycle phases.
- [ExampleGameplay](../Examples/ExampleGameplay/): Native game hooks and events.
- [UIDemo](../Examples/UIDemo/): Custom Mod Bar elements and native-styled UI panels.
