Foundation 0.7.0
By Urakka

A framework for Kingdom Inc modders, with Base Game and Mods tabs beneath
Tech Tree. Other plugins can register real native research on the Mods page.
The left Mod List groups technologies by their owning mod. Select a category
to view its research; each category remembers its branch, map position and zoom.
Crowded technologies automatically move into free space, including their cost
and requirement labels. The sidebar scrolls without zooming the map.

Native research appearance and behavior: game prefabs, tooltips, hover/glow,
locked/purchased states, costs, prerequisites, connectors and upgrade branches.
Uses the usual map pan/zoom and research list. Supports Better Zoom.
Click red Requires labels to jump to their prerequisite technology. The correct
branch and Base Game or Mods tab open automatically, without buying research.
Adds a public workspace registration API: native build menu, placement,
collisions, dragging, ports, copying and saved custom models.
No demonstration nodes are installed automatically; see the separate SDK.

The shared Mod Bar appears 2 pixels under the existing currency bar with a
top-center chevron toggle that smoothly slides the bar open and closed. Other
mods can add buttons, text, dropdowns, toggles, separators and custom controls
aligned left, center or right. It follows the game's UI scale and HUD
visibility, and scrolls horizontally when full. Foundation installs no
demonstration controls or Ledger-specific behavior.

Custom 3x Options / Config Menu: Foundation replaces the vanilla Options menu
with a spacious, 3x expanded design while retaining 100% of vanilla options
(Resolution, Fullscreen, Quality, Master/Music/SFX Audio volumes, Apply, Cancel).
A new [ Mod Settings ] tab lets players configure installed mods directly in-game.
Mods register sections via FoundationApi.ModConfig with native-styled buttons,
dropdowns, sliders, text headers, toggles, or direct BepInEx ConfigEntry bindings,
complete with Apply/Cancel lifecycle hooks.

REQUIREMENTS
Kingdom Inc (tested: Release v1.0.13).
BepInEx 5 (tested: 5.4.23.6), with its bundled Harmony library.

INSTALLATION
Extract this archive into the folder containing KingdomInc.exe.
Installed file: BepInEx/plugins/Foundation/Foundation.dll.
Relaunch Kingdom Inc and open Tech Tree -> Mods, or open Options -> Mod Settings.

UPDATING AND REMOVAL
Close the game before replacing/removing the Foundation plugin folder.
Dependent mods require Foundation to load.
Research progress uses the game's existing saved technology IDs. Missing IDs
are skipped when loading without their defining mod; saving again without
that mod may discard its research progress. No custom save schema is added.
Manual installation verified. Mod-manager deployment untested.

FOR MODDERS
Use the Foundation SDK 0.7.0 for the public interfaces, native research options,
Mod Bar contracts, Mod Config contracts, IntelliSense XML and a separate BepInEx
consumer example.

CREDITS
Uses the game's existing UI sprites and fonts at runtime.
No game assemblies or artwork are included in this package.

CUSTOM WORKSPACE NODE REMOVAL
Remove placed custom workspace nodes before uninstalling their defining mod
or Foundation. The native game loader requires their factory to load saves
containing those ElementType IDs. Native research IDs are handled differently.

The SDK includes WorkspaceNode and workspace registry interfaces. The optional
example includes a relay unlocked by its first technology, purchased for
20 gold through the native Utility build category.
