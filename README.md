# Foundation

Foundation 0.6.0 is a C# framework for Kingdom Inc mods. It adds **Base Game | Mods** beneath the **Tech Tree** heading and lets other plugins create real native research through public interfaces.

The shared **Mod Bar** sits 2 pixels beneath the existing currency bar with a top-center chevron toggle that smoothly slides the bar open and closed. Mods can register text, buttons, dropdowns, toggles, vertical separators or custom controls through `FoundationApi.ModBar`, aligned to the left, center or right. The registry, element, view, context and registration contracts are public interfaces; built-in controls are ordinary implementations, and custom implementations use the same path. Controls use native frames, fonts and dropdowns, follow HUD scaling/visibility, and scroll horizontally when space is limited. Each mod owns its callbacks and state; Foundation owns slot layout and scene cleanup. The player package includes the bar, with no demonstration controls.

Download the player package and separate developer SDK from [GitHub Releases](https://github.com/urakkaamyx/KingdomInc_Foundation/releases). For a source checkout, clone this repository into `<KingdomInc>/_Mods/Foundation`; game and BepInEx assemblies stay local.

Registered nodes use the game's actual research models, building/upgrade prefabs, fonts, tooltips, highlights, available glow, purchased checkmarks, cost displays, insufficient-funds feedback, prerequisites, connectors, upgrade branches and research list. The Mods map uses normal game zoom/pan controls, including Better Zoom. Each tab retains its camera and selected branch.

Click a red **Requires …** label to open the required technology's branch and center it on the map. Each missing requirement has its own destination, including links between Base Game and Mods. Navigation preserves the native appearance and does not purchase research.

The **Mods** tab has a **Mod List** on the left. Select a mod to view its own research map and list. Existing plugins are grouped automatically using their BepInEx identity; explicit category metadata is also supported. Each category remembers its branch, position and zoom. Empty categories disappear, and the sidebar scrolls independently of map zoom.

Technologies sharing a visible map cannot overlap: Foundation measures their native frames, costs and requirement panels and moves later nodes into free space. Definition coordinates remain the requested positions; the native model exposes the resolved positions used by focus and connectors. Separate categories can reuse coordinates.

## For modders

Use `FoundationApi.CreateTechNode(ITechNodeDefinition)` or `ITechNodeRegistry.CreateTechNode`. The returned ITechNode supports presentation updates, disposal and successful-unlock callbacks. INativeTechNode exposes the actual game model. TechNodeOptions configures native costs, prerequisites and branch relationships. The workspace API adds IWorkspaceNodeDefinition, IWorkspaceNodeRegistry, IWorkspaceNodeType and a WorkspaceNode base class. RegisterWorkspaceNode exposes a native building type; its factory supplies the mod-owned FlowElement behavior. Types appear in the normal build menu after research, and use native placement, dragging, collisions, ports, copying and save/load.

See [the API guide](SDK/README.md) and [the separately compiled example](Examples/ExampleTechNodes/ExamplePlugin.cs). The player package installs no example technologies; another plugin must register them. Version 0.3.0 replaces 0.2.0's separate overlay cards with the actual game research system. Rebuild prior SDK consumers; native positions use Y up and Clicked means successful native unlock.

## Requirements and installation

Tested against Kingdom Inc Release v1.0.12 / Unity 6000.3.27f2 and BepInEx 5.4.23.6 with bundled HarmonyX. Extract the player ZIP into the game directory so the DLL is at `BepInEx/plugins/Foundation/Foundation.dll`, then relaunch. Manual installation is verified; mod-manager installation is untested.

Close the game before replacing/removing an installed DLL. `install.ps1 -WhenGameCloses` can queue a verified update while the game is running; it backs up the previous DLL after the game closes. `-StageForNextLaunch` is only for a first installation. To remove, delete the Foundation plugin folder. Dependent mods require Foundation to load.

Research purchases use the game's existing saved technology IDs, with no custom save schema. Re-register the same IDs each launch to retain progress. The game skips missing technologies when loading without their defining mod; saving again without them may drop those IDs. Foundation keeps its branch notification counters out of saved NewTechCount dictionaries to avoid missing-key load failures.

## Build and verify

Keep this project at `<KingdomInc>/_Mods/Foundation`. Game assemblies stay local and are never packaged.

```powershell
dotnet build .\Foundation.csproj -c Release
dotnet build .\Examples\ExampleTechNodes\ExampleTechNodes.csproj -c Release
.\verify-isolated.ps1
.\verify-isolated.ps1 -WithExistingPlugins
.\install.ps1 -WhenGameCloses
.\package.ps1
```

Diagnostics use a disposable kingdom and a separate consumer plugin, blocking save writes, autosaves, latest-save loading and achievements. They exercise native prefab identity, appearance/status, real pointer purchases, insufficient funds, prerequisite unlocking, branch connectors, native JSON persistence, late registration/storage growth, automatic refresh, list filtering, pan/zoom, tab state and scene cleanup. Reports and screenshots are under ignored Verification. The compatibility run includes the currently installed plugins and a separate Factory API port. Tests cover this framework, not every behavior of every other mod.

The packager builds separate player and developer ZIPs, verifies clean extraction and compiles the consumer against the extracted SDK's DLL. It preserves prior release archives.

Plugin ID: `urakka.kingdominc.foundation`. Public name and assembly: Foundation.

UI sprites/fonts are referenced from the installed game at runtime, not redistributed. No game assemblies or artwork are packaged. Implementation, documentation and verification were developed with AI assistance under the author's direction. No source license is specified.

Workspace registrations are building types, not placed instances. The optional SDK example includes an input/output relay costing 20 gold, unlocked by its first research. The framework itself adds no demonstration nodes. See the SDK workspace section for a complete subclass and registration example.

Remove placed custom workspace nodes before uninstalling the defining plugin or Foundation: the native loader needs its registered factory to recreate saved FlowElement types. Deactivation during the current process hides new purchases while retaining existing instances and factory routing. Mods own custom data and port-schema migrations.

Verification includes separate consumer ownership detection, category isolation, actual sidebar button raycasts, category camera/branch restoration, cross-category prerequisite navigation, root/branch/resource collision layout, connector positions, live updates, stable layouts, sidebar overflow scrolling and native zoom blocking. Existing research and workspace checks remain in the same disposable runtime suite. Results are recorded in Verification and the release record; user saves are not loaded or written.
