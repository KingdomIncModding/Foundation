Foundation 0.10.3 - unified Core + Machines + UI

BepInEx 5 plugin for Kingdom Inc.
Installed file: BepInEx/plugins/Foundation/Foundation.dll

The single Foundation.dll includes the existing game/modding API, Core runtime,
and the merged Machine API (KingdomFoundation.Machines.FoundationMachineApi).
The existing Foundation and Foundation Core registration IDs remain for mod compatibility.
No Foundation.Machines.dll is required or supported in this merged release.

Install into the Kingdom Inc game directory with BepInEx 5 available.
Close the game before changing plugins. Back up your saves before testing.

Mods compiled against Foundation.Machines.dll must be rebuilt against Foundation.dll.
Only the unified Foundation.dll belongs in the normal player installation.

Developer note: Source/build integration does not replace runtime testing.
A successful Windows build and in-game verification are required before publishing.

Includes native-themed UI framework, owner-scoped windows, and machine editors.
